﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using MegaApp.Enums;
using MegaApp.Interfaces;
using MegaApp.ViewModels;
using MegaApp.ViewModels.Offline;
using MegaApp.ViewModels.SharedFolders;

namespace MegaApp.Converters
{
    /// <summary>
    /// Class to convert from a viewstate value to a Visibility state (Visible/Collapsed)
    /// </summary>
    public class NodeViewStateToVisibilityConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is OfflineNodeViewModel)
                return this.ConvertOfflineNode(value, targetType, parameter, language);

            var node = value as NodeViewModel;
            var parentFolder = node?.Parent;
            if (parentFolder == null) return Visibility.Collapsed;

            var containerType = parentFolder.Type;
            var command = parameter as string;
            switch (containerType)
            {
                case ContainerType.CloudDrive:
                case ContainerType.CameraUploads:
                    switch (command)
                    {
                        case "preview":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsImage ? 
                                Visibility.Visible : Visibility.Collapsed;

                        case "information":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "download":
                        case "copyormove":
                        case "remove":
                            return parentFolder.ItemCollection != null && parentFolder.ItemCollection.HasSelectedItems ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "getlink":
                        case "rename":
                            return parentFolder.ItemCollection.MoreThanOneSelected ? 
                                Visibility.Collapsed : Visibility.Visible;

                        case "removelink":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsExported ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "share":
                            return parentFolder.ItemCollection.MoreThanOneSelected || !node.IsFolder ?
                                Visibility.Collapsed : Visibility.Visible;

                        default:
                            return Visibility.Collapsed;
                    }

                case ContainerType.RubbishBin:
                    switch (command)
                    {
                        case "preview":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsImage ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "information":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "download":
                        case "copyormove":
                        case "remove":
                            return parentFolder.ItemCollection != null && parentFolder.ItemCollection.HasSelectedItems ?
                                Visibility.Visible : Visibility.Collapsed;
                        
                        case "rename":
                            return parentFolder.ItemCollection.MoreThanOneSelected ?
                                Visibility.Collapsed : Visibility.Visible;

                        case "restore":
                            if (parentFolder.ItemCollection == null || !parentFolder.ItemCollection.HasSelectedItems)
                                return Visibility.Collapsed;

                            foreach (var item in parentFolder.ItemCollection.SelectedItems)
                            {
                                var selectedItem = item as NodeViewModel;
                                if (selectedItem == null || !selectedItem.CanRestore)
                                    return Visibility.Collapsed;
                            }

                            return Visibility.Visible;

                        default:
                            return Visibility.Collapsed;
                    };

                case ContainerType.InShares:
                case ContainerType.ContactInShares:
                    switch (command)
                    {
                        case "download":
                            return Visibility.Visible;

                        case "remove":
                            if (parentFolder is SharedFoldersListViewModel)
                                return Visibility.Collapsed;

                            return parentFolder.FolderRootNode != null && parentFolder.FolderRootNode.HasFullAccessPermissions ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "rename":
                            if (parentFolder.ItemCollection == null || !parentFolder.ItemCollection.OnlyOneSelectedItem)
                                return Visibility.Collapsed;

                            if (parentFolder is SharedFoldersListViewModel)
                            {
                                var focusedItem = parentFolder.ItemCollection.FocusedItem as IMegaNode;
                                return focusedItem != null && focusedItem.HasFullAccessPermissions ?
                                    Visibility.Visible : Visibility.Collapsed;
                            }

                            return parentFolder.FolderRootNode != null && parentFolder.FolderRootNode.HasFullAccessPermissions ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "leaveshare":
                            return parentFolder is SharedFoldersListViewModel ?
                                Visibility.Visible : Visibility.Collapsed;

                        default:
                            return Visibility.Collapsed;
                    }

                case ContainerType.OutShares:
                    switch (command)
                    {
                        case "download":
                        case "remove":
                            return Visibility.Visible;

                        case "getlink":
                        case "rename":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "removelink":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsExported ?
                                Visibility.Visible : Visibility.Collapsed;

                        default:
                            return Visibility.Collapsed;
                    }
                
                case ContainerType.FolderLink:
                    switch (command)
                    {
                        case "preview":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem && node.IsImage ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "information":
                            return parentFolder.ItemCollection.OnlyOneSelectedItem ?
                                Visibility.Visible : Visibility.Collapsed;

                        case "download":
                        case "import":
                            return Visibility.Visible;

                        default:
                            return Visibility.Collapsed;
                    }
            }

            return Visibility.Collapsed;
        }

        private object ConvertOfflineNode(object value, Type targetType, object parameter, string language)
        {
            var node = value as OfflineNodeViewModel;
            var parentFolder = node?.Parent;
            if (parentFolder == null) return Visibility.Collapsed;

            var command = parameter as string;
            switch (command)
            {
                case "remove":
                    return parentFolder.ItemCollection != null && parentFolder.ItemCollection.HasSelectedItems ?
                                Visibility.Visible : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
