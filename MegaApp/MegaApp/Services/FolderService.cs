﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.System;
using mega;
using MegaApp.ViewModels;

namespace MegaApp.Services
{
    static class FolderService
    {
        /// <summary>
        /// Determines if exists the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder exists or FALSE in other case</returns>
        public static bool FolderExists(string path)
        {  
            return Directory.Exists(path);
        }

        /// <summary>
        /// Gets the number of child folders of the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>Number of child folders</returns>
        public static int GetNumChildFolders(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return 0;
                return Directory.GetDirectories(path).Length;
            }
            catch (Exception) { return 0; }            
        }

        /// <summary>
        /// Gets the number of child files of the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <param name="isOfflineFolder">Boolean value which indicates if is an "offline" folder</param>
        /// <returns>Number of child files</returns>
        public static int GetNumChildFiles(string path, bool isOfflineFolder = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return 0;

                string[] childFiles = Directory.GetFiles(path);
                if (childFiles == null) return 0;

                int num = 0;
                if (!isOfflineFolder)
                {
                    num = childFiles.Length;
                }
                else
                {
                    foreach (var filePath in childFiles)
                        if (!FileService.IsPendingTransferFile(Path.GetFileName(filePath))) num++;
                }

                return num;
            }
            catch (Exception) { return 0; }
        }

        /// <summary>
        /// Determines if the spedified folder is an empty folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder is empty or FALSE in other case</returns>
        public static bool IsEmptyFolder(string path)
        {
            return (Directory.GetDirectories(path).Count() == 0 && Directory.GetFiles(path).Count() == 0) ? true : false;
        }

        /// <summary>
        /// Creates the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <returns>TRUE if the folder was created successfully or FALSE in other case</returns>
        public static bool CreateFolder(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch(Exception) { return false; }
        }

        /// <summary>
        /// Deletes the specified folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        /// <param name="recursive">Boolean value which indicates if the deletion should be recursive</param>
        /// <returns>TRUE if the deletion was well or FALSE in other case</returns>
        public static bool DeleteFolder(string path, bool recursive = false)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    Directory.Delete(path, recursive);
                return true;
            }
            catch (Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR,
                    string.Format("Error deleting folder '{0}'", path), e);
                return false;
            }
        }

        /// <summary>
        /// Determines if a folder path contains illegal characters
        /// </summary>
        /// <param name="path">Path of the folder to check</param>
        /// <returns>TRUE if has illegal chars or FALSE in other case</returns>
        public static bool HasIllegalChars(string path)
        {
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
            {
                if (path.Contains(c.ToString())) return true;
            }
            return false;
        }

        /// <summary>
        /// Empties a folder
        /// </summary>
        /// <param name="path">Path of the folder</param>
        public static async Task<bool> ClearAsync(string path)
        {
            try
            {
                if (HasIllegalChars(path))
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_WARNING, string.Format("Error cleaning folder '{0}'.", path));
                    return false;
                }

                bool result = true;

                await Task.Run(async() =>
                {
                    IEnumerable<string> foldersToDelete = Directory.GetDirectories(path);
                    if (foldersToDelete != null)
                    {
                        foreach (var folder in foldersToDelete)
                        {
                            if (folder == null) continue;

                            if (HasIllegalChars(folder))
                            {
                                LogService.Log(MLogLevel.LOG_LEVEL_WARNING, string.Format("Error deleting folder '{0}'.", path));
                                result = false;
                                continue;
                            }

                            Directory.Delete(folder, true);
                        }
                    }

                    result = result & await FileService.ClearFilesAsync(Directory.GetFiles(path));
                });

                return result;
            }
            catch (Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error cleaning folder.", e);
                return false;
            }
        }

        /// <summary>
        /// Copy a folder to a specified folder and allow rename the destination folder
        /// </summary>
        /// <param name="srcFolderPath">Path of the source folder</param>
        /// <param name="destFolderPath">Path of the destination folder for the copied folder</param>
        /// <param name="folderNewName">New name for the folder</param>
        /// <param name="isForMove">Indicate if the copy is part of a move action</param>
        /// <returns>TRUE if the folder was copied or FALSE if something failed</returns>
        public static async Task<bool> CopyFolderAsync(string srcFolderPath, string destFolderPath,
            string folderNewName = null, bool isForMove = false)
        {
            try
            {
                DirectoryInfo srcFolder = new DirectoryInfo(srcFolderPath);
                if (!srcFolder.Exists)
                {
                    string errorMessage = "Source folder does not exist or could not be found: " + srcFolderPath;
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, errorMessage);
                    return false;
                }

                folderNewName = folderNewName ?? srcFolder.Name;
                destFolderPath = Path.Combine(destFolderPath, folderNewName);
                                
                // If the destination directory doesn't exist, create it.
                if (!Directory.Exists(destFolderPath))
                    Directory.CreateDirectory(destFolderPath);

                bool result = true;

                // Get the files in the folder and copy them to the new location.
                FileInfo[] files = srcFolder.GetFiles();
                foreach (FileInfo file in files)
                    result &= await FileService.CopyFileAsync(file.FullName, destFolderPath, file.Name);

                // Get the subfolders in the folder and copy them to the new location.
                DirectoryInfo[] subfolders = srcFolder.GetDirectories();
                foreach (DirectoryInfo subfolder in subfolders)
                    result &= await CopyFolderAsync(subfolder.FullName, destFolderPath, subfolder.Name);

                if (!result && !isForMove)
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error copying folder:");
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Source: " + srcFolderPath);
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Destination: " + destFolderPath);
                }

                return result;
            }
            catch (Exception e)
            {
                if (!isForMove)
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error copying folder:", e);
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Source: " + srcFolderPath);
                    LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Destination: " + destFolderPath);
                }

                return false;
            }
        }

        /// <summary>
        /// Move a folder to a specified folder and allow rename the destination folder.
        /// Copy the folder and remove the source folder if the copy was successful.
        /// </summary>
        /// <param name="srcFolderPath">Path of the source folder</param>
        /// <param name="destFolderPath">Path of the destination folder for the moved folder</param>
        /// <param name="folderNewName">New name for the folder</param>
        /// <returns>TRUE if the folder was moved or FALSE if something failed</returns>
        public static async Task<bool> MoveFolderAsync(string srcFolderPath, string destFolderPath, string folderNewName = null)
        {
            try
            {
                bool result = true;
                result &= await CopyFolderAsync(srcFolderPath, destFolderPath, folderNewName, true);
                result &= DeleteFolder(srcFolderPath, true);
                return result;
            }
            catch (Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error moving folder:", e);
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Source: " + srcFolderPath);
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Destination: " + destFolderPath);
                return false;
            }
        }

        /// <summary>
        /// Open a folder in the file explorer
        /// </summary>
        /// <param name="folderPath">Path of the folder to open</param>
        /// <returns>TRUE if the folder could be opened or FALSE if something failed</returns>
        public static async Task<bool> OpenFolder(string folderPath)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
                return await Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception e)
            {
                LogService.Log(MLogLevel.LOG_LEVEL_ERROR, "Error opening folder", e);
                UiService.OnUiThread(async () =>
                {
                    await DialogService.ShowAlertAsync(
                       ResourceService.AppMessages.GetString("AM_OpenFolderFailed_Title"),
                       ResourceService.AppMessages.GetString("AM_OpenFolderFailed"));
                });

                return false;
            }
        }

        public static async Task<StorageFolder> SelectFolder()
        {
            try
            {
                var folderPicker = new FolderPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.Downloads,
                    CommitButtonText = ResourceService.UiResources.GetString("UI_Download")
                };
                folderPicker.FileTypeFilter.Add("*");

                var folder = await folderPicker.PickSingleFolderAsync();

                if(folder != null)
                    Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);

                return folder;
            }
            catch (Exception e)
            {
                UiService.OnUiThread(async() =>
                {
                    await DialogService.ShowAlertAsync(
                        ResourceService.AppMessages.GetString("AM_SelectFolderFailed_Title"),
                        string.Format(ResourceService.AppMessages.GetString("AM_SelectFolderFailed"), e.Message));
                });

                return null;
            }
        }

        /// <summary>
        /// Update information of all folder nodes in a folder view
        /// </summary>
        /// <param name="folder">Folder view to update</param>
        public static void UpdateFolders(FolderViewModel folder)
        {
            foreach (var folderNode in folder.ItemCollection.Items
                .Where(f => f is FolderNodeViewModel)
                .Cast<FolderNodeViewModel>()
                .ToList())
            {
                folderNode.SetFolderInfo();
            }
        }

        /// <summary>
        /// Check if a path is the root of the offline folder
        /// </summary>
        /// <param name="path">Path to check</param>
        /// <returns>TRUE if is the root of the offline folder or FALSE in other case</returns>
        public static bool IsOfflineRootFolder(string path) =>
            string.CompareOrdinal(AppService.GetOfflineDirectoryPath(), path) == 0;

        /// <summary>
        /// Get the size of a folder
        /// </summary>
        /// <param name="folderPath">Path of the folder</param>
        /// <returns>Folder size</returns>
        public static async Task<ulong> GetFolderSizeAsync(string folderPath)
        {
            ulong totalSize = 0;

            if (string.IsNullOrWhiteSpace(folderPath) && !Directory.Exists(folderPath))
                return totalSize;

            await Task.Run(async() =>
            {
                var folders = new List<string>();
                try { folders.AddRange(Directory.GetDirectories(folderPath)); }
                catch (Exception e)
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_WARNING,
                        string.Format("Error getting the subfolder list from {0}", folderPath), e);
                }

                foreach (var folder in folders)
                    totalSize += await GetFolderSizeAsync(folder);

                var files = new List<string>();                
                try { files.AddRange(Directory.GetFiles(folderPath)); }
                catch (Exception e)
                {
                    LogService.Log(MLogLevel.LOG_LEVEL_WARNING, 
                        string.Format("Error getting the file list from {0}", folderPath), e);
                }

                foreach (var file in files)
                {
                    if (!FileService.FileExists(file)) continue;

                    try { totalSize += (ulong)new FileInfo(file).Length; }
                    catch (Exception e)
                    {
                        LogService.Log(MLogLevel.LOG_LEVEL_WARNING,
                                string.Format("Error getting file size of {0}", file), e);
                    }
                }
            });

            return totalSize;
        }
    }    
}
