﻿using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using mega;
using BackgroundTaskService.MegaApi;

namespace BackgroundTaskService.Services
{
    internal static class SdkService
    {
        /// <summary>
        /// Main MegaSDK instance of the app
        /// </summary>
        private static MegaSDK _megaSdk;
        public static MegaSDK MegaSdk
        {
            get
            {
                if (_megaSdk != null) return _megaSdk;
                _megaSdk = CreateSdk();
                return _megaSdk;
            }
        }
        
        /// <summary>
        /// Initialize all the SDK parameters
        /// </summary>
        public static void InitializeSdkParams()
        {
            //The next line enables a custom logger, if this function is not used OutputDebugString() is called
            //in the native library and log messages are only readable with the native debugger attached.
            //The default behavior of MegaLogger() is to print logs using Debug.WriteLine() but it could
            //be used to sends log to a file, for example.
            LogService.AddLoggerObject(LogService.MegaLogger);

            //You can select the maximum output level for debug messages.
            //By default FATAL, ERROR, WARNING and INFO will be enabled
            //DEBUG and MAX can only be enabled in Debug builds, they are ignored in Release builds
            MegaSDK.setLogLevel(MLogLevel.LOG_LEVEL_DEBUG);

            //You can send messages to the logger using MEGASDK.log(), those messages will be received
            //in the active logger
            MegaSDK.log(MLogLevel.LOG_LEVEL_INFO, "Example log message");

            // Set the ID for statistics
            MegaSDK.setStatsID(GetDeviceId());
        }

        /// <summary>
        /// Create a MegaSDK instance
        /// </summary>
        /// <returns>The new MegaSDK instance</returns>
        private static MegaSDK CreateSdk()
        {
            // Create Camera Upload service directory if not already exists
            CreateCameraUploadServiceFolder();
            
            // Get an instance of the object that allow recover the local device information.
            var deviceInfo = new EasClientDeviceInformation();

            // Initialize a MegaSDK instance
            var newMegaSDK = new MegaSDK(
                "Z5dGhQhL",
                string.Format("{0}/{1}/{2}",
                    string.Format("MEGA_UWP/UploadService/{0}", GetTaskVersion()),
                    deviceInfo.SystemManufacturer,
                    deviceInfo.SystemProductName),
                CameraUploadsServiceFolder,
                new MegaRandomNumberProvider());

            // Use custom DNS servers in the new SDK instance
            SetDnsServers(newMegaSDK, false);

            return newMegaSDK;
        }

        /// <summary>
        /// Use custom DNS servers in the selected SDK instance.
        /// </summary>
        /// <param name="megaSdk">SDK instance to set the custom DNS servers.</param>
        /// <param name="refresh">Indicates if should refresh the previously stored addresses.</param>
        private static async void SetDnsServers(MegaSDK megaSdk, bool refresh = true)
        {
            var dnsServers = NetworkService.GetSystemDnsServers(refresh);
            if (string.IsNullOrWhiteSpace(dnsServers))
                dnsServers = await NetworkService.GetMegaDnsServersAsync(refresh);
            if (!string.IsNullOrWhiteSpace(dnsServers))
                megaSdk.setDnsServers(dnsServers);
        }

        /// <summary>
        /// Use custom DNS servers in all the SDK instances.
        /// </summary>
        /// <param name="refresh">Indicates if should refresh the previously stored addresses.</param>
        public static async void SetDnsServers(bool refresh = true)
        {
            var dnsServers = NetworkService.GetSystemDnsServers(refresh);
            if (string.IsNullOrWhiteSpace(dnsServers))
                dnsServers = await NetworkService.GetMegaDnsServersAsync(refresh);

            if (!string.IsNullOrWhiteSpace(dnsServers))
                MegaSdk.setDnsServers(dnsServers);
        }

        /// <summary>
        /// Create the Camera Upload service directory if not already exists
        /// </summary>
        private static async void CreateCameraUploadServiceFolder()
        {
            try
            {
                try
                {
                    if (Directory.Exists(CameraUploadsServiceFolder)) return;
                    Directory.CreateDirectory(CameraUploadsServiceFolder);
                }
                catch (Exception e)
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_WARNING, "Error creating the 'Camera Upload' service folder", e);
                    LogService.Log(MLogLevel.LOG_LEVEL_INFO, "Retrying to create the 'Camera Upload' service folder...");
                    await ApplicationData.Current.LocalFolder.CreateFolderAsync("CameraUploadService", CreationCollisionOption.OpenIfExists);
                }
            }
            catch(Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error creating the 'Camera Upload' service folder", e);
            }
        }

        public static bool IsAlreadyUploaded(StorageFile fileToUpload, Stream fileStream, MNode rootNode, ulong mTime)
        {
            // Make sure the stream pointer is at the start of the stream
            fileStream.Position = 0;
            // Get the unique fingerprint of the file
            string fingerprint = MegaSdk.getFileFingerprint(new MegaInputStream(fileStream), mTime);
            // Check if the fingerprint is already in the subfolders of the Camera Uploads
            var mNode = MegaSdk.getNodeByFingerprint(fingerprint, rootNode);

            if (mNode == null) return false;

            if (MegaSdk.isInCloud(mNode))
            {
                var parent = MegaSdk.getParentNode(mNode);
                return parent?.getHandle() == rootNode.getHandle();
            }
            
            return !MegaSdk.isInRubbish(mNode);
        }

        /// <summary>
        /// Locate the Camera Uploads folder node to use as parent for the uploads
        /// </summary>
        /// <returns>Camera Uploads root folder node</returns>
        public static async Task<MNode> GetCameraUploadRootNodeAsync()
        {
            // First try to retrieve the Cloud Drive root node
            var rootNode = MegaSdk.getRootNode();
            if (rootNode == null) return null;

            // Locate the camera upload node
            var cameraUploadNode = FindCameraUploadNode(rootNode);

            // If node found, return the node
            if (cameraUploadNode != null) return cameraUploadNode;

            // If node not found, create a new Camera Uploads node
            var folder = new MegaRequestListener<bool>();
            var result = await folder.ExecuteAsync(() => MegaSdk.createFolder("Camera Uploads", rootNode, folder));
            return result ? FindCameraUploadNode(rootNode) : null;
        }

        /// <summary>
        /// Locate the Camera Uploads folder node in the specified root
        /// </summary>
        /// <param name="rootNode">Current root node</param>
        /// <returns>Camera Uploads folder node in</returns>
        private static MNode FindCameraUploadNode(MNode rootNode)
        {
            var childs = MegaSdk.getChildren(rootNode);

            for (var x = 0; x < childs.size(); x++)
            {
                var node = childs.get(x);
                // Camera Uploads is a folder
                if (node.getType() != MNodeType.TYPE_FOLDER) continue;
                // Check the folder name
                if (!node.getName().ToLower().Equals("camera uploads")) continue;
                return node;
            }

            return null;
        }

        private static string GetTaskVersion()
        {
            return "4.0.0";
        }

        private static string GetDeviceId()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;

            HashAlgorithmProvider hasher = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer hashed = hasher.HashData(hardwareId);

            string hashedString = CryptographicBuffer.EncodeToHexString(hashed);
            return hashedString;
        }

        private static string CameraUploadsServiceFolder => 
            Path.Combine(ApplicationData.Current.LocalFolder.Path, "CameraUploadService");
    }
}
