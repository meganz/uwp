﻿using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using mega;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Extensions;
using MegaApp.Interfaces;
using MegaApp.Services;

namespace MegaApp.ViewModels
{
    /// <summary>
    /// ViewModel of the main MEGA datatype (MNode)
    /// </summary>
    public abstract class NodeViewModel : BaseSdkViewModel, IMegaNode
    {
        // Offset DateTime value to calculate the correct creation and modification time
        private static readonly DateTime OriginalDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        protected NodeViewModel(MegaSDK megaSdk, AppInformation appInformation, MNode megaNode, ContainerType parentContainerType,
            ObservableCollection<IMegaNode> parentCollection = null, ObservableCollection<IMegaNode> childCollection = null)
        {
            Update(megaNode, parentContainerType);
            SetDefaultValues();

            this.ParentCollection = parentCollection;
            this.ChildCollection = childCollection;
        }

        #region Private Methods

        private void SetDefaultValues()
        {
            this.IsMultiSelected = false;
            this.DisplayMode = NodeDisplayMode.Normal;

            if (this.Type == MNodeType.TYPE_FOLDER) return;

            //if (FileService.FileExists(ThumbnailPath))
            //{
            //    this.IsDefaultImage = false;
            //    this.ThumbnailImageUri = new Uri(ThumbnailPath);
            //}
            //else
            //{
                this.IsDefaultImage = true;
                this.DefaultImagePathData = ImageService.GetDefaultFileTypePathData(this.Name);
            //}
        }

        /// <summary>
        /// Convert the MEGA time to a C# DateTime object in local time
        /// </summary>
        /// <param name="time">MEGA time</param>
        /// <returns>DateTime object in local time</returns>
        private static DateTime ConvertDateToString(ulong time)
        {
            return OriginalDateTime.AddSeconds(time).ToLocalTime();
        }

        #endregion

        #region IBaseNode Interface

        public string Name { get; set; }

        public string CreationTime { get; private set; }

        public string ModificationTime { get; private set; }

        public string ThumbnailPath { get; }

        private string _information;
        public string Information
        {
            get { return _information; }
            set { SetField(ref _information, value); }
        }

        public string Base64Handle { get; set; }

        public ulong Size { get; set; }

        private string _sizeText;
        public string SizeText
        {
            get { return _sizeText; }
            set { SetField(ref _sizeText, value); }
        }

        public bool IsMultiSelected { get; set; }

        public bool IsFolder { get; }

        public bool IsImage { get; }

        public bool IsDefaultImage { get; set; }

        public Uri ThumbnailImageUri { get; set; }

        private string _defaultImagePathData;
        public string DefaultImagePathData
        {
            get { return _defaultImagePathData; }
            set { SetField(ref _defaultImagePathData, value); }
        }

        #endregion

        #region IMegaNode Interface

        public NodeActionResult Rename()
        {
            return NodeActionResult.IsBusy;
        }
                
        public NodeActionResult Move(IMegaNode newParentNode)
        {
            return NodeActionResult.IsBusy;
        }

        public NodeActionResult Copy(IMegaNode newParentNode)
        {
            return NodeActionResult.IsBusy;
        }

        public async Task<NodeActionResult> RemoveAsync(bool isMultiRemove, AutoResetEvent waitEventRequest = null)
        {
            return NodeActionResult.IsBusy;
        }

        public async Task<NodeActionResult> DeleteAsync()
        {
            return NodeActionResult.IsBusy;
        }

        public NodeActionResult GetLink()
        {
            return NodeActionResult.IsBusy;
        }

        public void Download(TransferQueu transferQueu, string downloadPath = null)
        {

        }

        public void Update(MNode megaNode, ContainerType parentContainerType)
        {
            OriginalMNode = megaNode;
            this.Handle = megaNode.getHandle();
            this.Base64Handle = megaNode.getBase64Handle();
            this.Type = megaNode.getType();
            this.ParentContainerType = parentContainerType;
            this.Name = megaNode.getName();
            this.Size = MegaSdk.getSize(megaNode);
            this.SizeText = this.Size.ToStringAndSuffix();
            this.IsExported = megaNode.isExported();
            this.CreationTime = ConvertDateToString(megaNode.getCreationTime()).ToString("dd MMM yyyy");

            if (this.Type == MNodeType.TYPE_FILE)
                this.ModificationTime = ConvertDateToString(megaNode.getModificationTime()).ToString("dd MMM yyyy");
            else
                this.ModificationTime = this.CreationTime;

            //if (!App.MegaSdk.isInShare(megaNode) && this.ParentContainerType != ContainerType.PublicLink &&
            //    this.ParentContainerType != ContainerType.InShares && this.ParentContainerType != ContainerType.ContactInShares &&
            //    this.ParentContainerType != ContainerType.FolderLink)
            //    CheckAndUpdateSFO(megaNode);
            this.IsAvailableOffline = false;
            this.IsSelectedForOffline = false;
        }

        public void SetThumbnailImage()
        {

        }

        public void Open()
        {

        }

        public ulong Handle { get; set; }

        public ObservableCollection<IMegaNode> ParentCollection { get; set; }

        public ObservableCollection<IMegaNode> ChildCollection { get; set; }

        public MNodeType Type { get; private set; }

        public ContainerType ParentContainerType { get; private set; }

        public NodeDisplayMode DisplayMode { get; set; }

        private bool _isSelectedForOffline;
        public bool IsSelectedForOffline
        {
            get { return _isSelectedForOffline; }
            set
            {
                SetField(ref _isSelectedForOffline, value);
                IsSelectedForOfflineText = _isSelectedForOffline ? 
                    ResourceService.UiResources.GetString("UI_On") : ResourceService.UiResources.GetString("UI_Off");
            }
        }

        private String _isSelectedForOfflineText;
        public String IsSelectedForOfflineText
        {
            get { return _isSelectedForOfflineText; }
            set
            {
                SetField(ref _isSelectedForOfflineText, value);
            }
        }

        private bool _isAvailableOffline;
        public bool IsAvailableOffline
        {
            get { return _isAvailableOffline; }
            set
            {
                SetField(ref _isAvailableOffline, value);
            }
        }

        private bool _isExported;
        public bool IsExported
        {
            get { return _isExported; }
            set { SetField(ref _isExported, value); }
        }

        public TransferObjectModel Transfer { get; set; }

        public MNode OriginalMNode { get; private set; }

        #endregion
    }
}
