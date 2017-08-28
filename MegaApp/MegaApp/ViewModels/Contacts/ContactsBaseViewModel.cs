﻿using System;
using System.Linq;
using System.ComponentModel;
using System.Windows.Input;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Services;

namespace MegaApp.ViewModels.Contacts
{
    public class ContactsBaseViewModel<T> : BaseSdkViewModel
    {
        public ContactsBaseViewModel(bool? isOutgoing = null)
        {
            this.isOutgoing = isOutgoing;

            this.MultiSelectCommand = new RelayCommand(MultiSelect);
            this.SelectionChangedCommand = new RelayCommand(SelectionChanged);
        }

        #region Events

        /// <summary>
        /// Event triggered when the add contact menu option is tapped
        /// </summary>
        public event EventHandler AddContactTapped;

        /// <summary>
        /// Event invocator method called when the add contact menu option is tapped
        /// </summary>
        protected virtual void OnAddContactTapped()
        {
            this.AddContactTapped?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event triggered when the multi select scenario is enabled
        /// </summary>
        public event EventHandler MultiSelectEnabled;

        /// <summary>
        /// Event invocator method called when the multi select scenario is enabled
        /// </summary>
        protected virtual void OnMultiSelectEnabled()
        {
            this.MultiSelectEnabled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Event triggered when the multi select scenario is disabled
        /// </summary>
        public event EventHandler MultiSelectDisabled;

        /// <summary>
        /// Event invocator method called when the multi select scenario is disabled
        /// </summary>
        protected virtual void OnMultiSelectDisabled()
        {
            this.MultiSelectDisabled?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Commands
        
        public ICommand MultiSelectCommand { get; }
        public ICommand SelectionChangedCommand { get; }

        #endregion

        #region Methods

        private void ListOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.List.Items))
            {
                OnPropertyChanged(nameof(this.OrderTypeAndNumberOfItems));
                OnPropertyChanged(nameof(this.OrderTypeAndNumberOfSelectedItems));
            }
        }

        private void SelectionChanged()
        {
            if (DeviceService.GetDeviceType() == DeviceFormFactorType.Desktop)
                this.IsMultiSelectActive = (this.IsMultiSelectActive && this.List.OneOrMoreSelected) ||
                    this.List.MoreThanOneSelected;
            else
                this.IsMultiSelectActive = this.IsMultiSelectActive && this.List.OneOrMoreSelected;

            if (this.List.HasSelectedItems)
            {
                var focusedItem = this.List.SelectedItems.Last();
                this.FocusedItem = focusedItem;
                OnPropertyChanged(nameof(this.OrderTypeAndNumberOfSelectedItems));
            }
        }

        /// <summary>
        /// Sets if multiselect is active or not.
        /// </summary>
        private void MultiSelect() => this.IsMultiSelectActive = !this.IsMultiSelectActive;

        #endregion

        #region Properties

        private bool? isOutgoing { get; set; }

        private CollectionViewModel<T> _list;
        public CollectionViewModel<T> List
        {
            get { return _list; }
            set
            {
                if (_list != null)
                    _list.PropertyChanged -= ListOnPropertyChanged;

                SetField(ref _list, value);

                if (_list != null)
                    _list.PropertyChanged += ListOnPropertyChanged;
            }
        }

        private T _focusedItem;
        public T FocusedItem
        {
            get { return _focusedItem; }
            set { SetField(ref _focusedItem, value); }
        }

        private ContactsViewState _viewState;
        public ContactsViewState ViewState
        {
            get { return _viewState; }
            set { SetField(ref _viewState, value); }
        }

        public string OrderTypeAndNumberOfItems
        {
            get
            {
                switch(this.CurrentOrder)
                {
                    case ContactsSortOptions.EmailAscending:
                    case ContactsSortOptions.EmailDescending:
                        return string.Format(ResourceService.UiResources.GetString("UI_ListSortedByEmail"), 
                            this.List.Items.Count);

                    case ContactsSortOptions.NameAscending:
                    case ContactsSortOptions.NameDescending:
                        return string.Format(ResourceService.UiResources.GetString("UI_ListSortedByName"),
                            this.List.Items.Count);

                    default:
                        return string.Empty;
                }
            }
        }

        public string OrderTypeAndNumberOfSelectedItems
        {
            get
            {
                switch (this.CurrentOrder)
                {
                    case ContactsSortOptions.EmailAscending:
                    case ContactsSortOptions.EmailDescending:
                        return string.Format(ResourceService.UiResources.GetString("UI_ListSortedByEmailMultiSelect"),
                            this.List.SelectedItems.Count, this.List.Items.Count);

                    case ContactsSortOptions.NameAscending:
                    case ContactsSortOptions.NameDescending:
                        return string.Format(ResourceService.UiResources.GetString("UI_ListSortedByNameMultiSelect"),
                            this.List.SelectedItems.Count, this.List.Items.Count);

                    default:
                        return string.Empty;
                }
            }
        }

        private bool _isMultiSelectActive;
        public bool IsMultiSelectActive
        {
            get { return _isMultiSelectActive || this.List.MoreThanOneSelected; }
            set
            {
                if (!SetField(ref _isMultiSelectActive, value)) return;

                if (_isMultiSelectActive)
                {
                    if (this.isOutgoing == null)
                        this.ViewState = ContactsViewState.ContactsMultiSelect;
                    else
                        this.ViewState = this.isOutgoing == true ? ContactsViewState.OutgoingRequestsMultiSelect :
                            ContactsViewState.IncomingRequestsMultiSelect;

                    this.OnMultiSelectEnabled();
                }
                else
                {
                    if (this.isOutgoing == null)
                        this.ViewState = ContactsViewState.Contacts;
                    else
                        this.ViewState = this.isOutgoing == true ? ContactsViewState.OutgoingRequests :
                            ContactsViewState.IncomingRequests;

                    this.List.ClearSelection();
                    OnPropertyChanged("IsMultiSelectActive");
                    this.OnMultiSelectDisabled();
                }
            }
        }

        private ContactsSortOptions _currentOrder;
        public ContactsSortOptions CurrentOrder
        {
            get { return _currentOrder; }
            set
            {
                SetField(ref _currentOrder, value);

                OnPropertyChanged(nameof(this.IsCurrentOrderAscending));
                OnPropertyChanged(nameof(this.OrderTypeAndNumberOfItems));
                OnPropertyChanged(nameof(this.OrderTypeAndNumberOfSelectedItems));
            }
        }

        public bool IsCurrentOrderAscending
        {
            get
            {
                switch(this.CurrentOrder)
                {
                    case ContactsSortOptions.EmailAscending:
                    case ContactsSortOptions.NameAscending:
                    default:
                        return true;

                    case ContactsSortOptions.EmailDescending:
                    case ContactsSortOptions.NameDescending:
                        return false;
                }
            }
        }

        #endregion

        #region UiResources

        public string AddContactText => ResourceService.UiResources.GetString("UI_AddContact");
        public string CancelText => ResourceService.UiResources.GetString("UI_Cancel");
        public string MultiSelectText => ResourceService.UiResources.GetString("UI_MultiSelect");
        public string SortByText => ResourceService.UiResources.GetString("UI_SortBy");

        #endregion

        #region VisualResources

        public string AddContactPathData => ResourceService.VisualResources.GetString("VR_AddContactPathData");
        public string CancelPathData => ResourceService.VisualResources.GetString("VR_CancelPathData");
        public string MultiSelectPathData => ResourceService.VisualResources.GetString("VR_MultiSelectPathData");
        public string SortByPathData => ResourceService.VisualResources.GetString("VR_SortByPathData");

        #endregion
    }
}
