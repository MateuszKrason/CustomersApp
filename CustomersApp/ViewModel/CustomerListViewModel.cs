﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Bogus.DataSets;
using CustomersApp.Model;
using CustomersApp.ViewModel.Commands;
using Microsoft.Win32;

namespace CustomersApp.ViewModel;

public class CustomerListViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Customer> Customers { get; set; }

    public string CustomerFilter { get; set; }

    private CollectionView _collectionView;

    public CollectionView CollectionView
    {
        get
        {
            _collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Customers);
            return _collectionView;
        }
    }

    private Customer? _selectedCustomer;

    public Customer? SelectedCustomer
    {
        get { return _selectedCustomer; }
        set
        {
            _selectedCustomer = value;
            SelectedCustomerState = $"{_selectedCustomer.Name} {_selectedCustomer.Surname}";
            OnPropertyChanged(nameof(SelectedCustomer));
        }
    }

    private string _selectedCustomerState;

    public string SelectedCustomerState
    {
        get { return _selectedCustomerState; }
        set
        {
            _selectedCustomerState = value;
            OnPropertyChanged(nameof(SelectedCustomerState));
        }
    }

    public ICommand SortListViewCommand { get; set; }
    public ICommand RefreshCustomersCommand { get; set; }
    public ICommand DeleteCustomerCommand { get; set; }
    public ICommand GeneratePdfCommand { get; set; }
    public ICommand UpdateCustomerCommand { get; set; }

    private CustomerService _customerService;
    private PdfService _pdfService;

    private const string StateNoData = "Nie wybrano danych";

    public CustomerListViewModel()
    {
        CustomerFilter = string.Empty;
        _customerService = ServiceProvider.CustomerServiceInstance();
        _pdfService = ServiceProvider.PdfServiceInstance();
        SortListViewCommand = new SortListViewCommand(this);
        RefreshCustomersCommand = new RefreshCustomersCommand(this);
        DeleteCustomerCommand = new DeleteCustomerCommand(this);
        GeneratePdfCommand = new GeneratePdfCommand(this);
        UpdateCustomerCommand = new UpdateCustomerCommand(this);
        Customers = new ObservableCollection<Customer>(_customerService.FindAll());
        CollectionView.Filter = FilterCustomers;
        SelectedCustomerState = StateNoData;
    }

    public void SortCustomers(string columnName)
    {
        SortDescription newSortDescription = new SortDescription(columnName, ListSortDirection.Ascending);

        if (CollectionView.SortDescriptions.Count > 0)
        {
            SortDescription oldSortDescription = CollectionView.SortDescriptions[0];
            if (oldSortDescription.PropertyName.Equals(columnName))
            {
                newSortDescription = new SortDescription(
                    columnName,
                    oldSortDescription.Direction == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending
                );
            }
        }

        CollectionView.SortDescriptions.Clear();
        CollectionView.SortDescriptions.Add(newSortDescription);
    }

    private bool FilterCustomers(object obj)
    {
        if (obj is Customer customer)
        {
            return customer.Name.Contains(CustomerFilter, StringComparison.InvariantCultureIgnoreCase) ||
                   customer.Surname.Contains(CustomerFilter, StringComparison.InvariantCultureIgnoreCase) ||
                   (customer.Name + " " + customer.Surname).Contains(CustomerFilter,
                       StringComparison.InvariantCultureIgnoreCase) ||
                   (customer.Surname + " " + customer.Name).Contains(CustomerFilter,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void RefreshCustomers()
    {
        _collectionView.Refresh();
    }

    public void ReloadCustomers()
    {
        List<Customer> customerList = _customerService.FindAll();
        Customers.Clear();
        foreach (Customer customer in customerList)
        {
            Customers.Add(customer);
        }
    }

    public void DeleteSelectedCustomer()
    {
        if (_selectedCustomer != null)
        {
            MessageBoxResult result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć zmarłego: {_selectedCustomer.Name} {_selectedCustomer.Surname} z bazy danych?",
                "Potwierdzenie usunięcia", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _customerService.DeleteCustomer(_selectedCustomer.Id);
                RefreshCustomers();
                ReloadCustomers();
                SelectedCustomer = EmptyCustomer();
                SelectedCustomerState = StateNoData;
            }
        }
    }

    private Customer EmptyCustomer() => new Customer()
    {
        Id = -1,
        Name = "",
        Surname = "",
        CertificateNumber = "",
        Sex = ' ',
        DateOfBirth = null,
        PlaceOfBirth = "",
        DateOfDeath = null,
        PlaceOfDeath = "",
        IssueDate = null,
        DeathCertificateNumber = "",
        IssuedBy = "",
        Address = ""
    };

    public void GeneratePdf()
    {
        if (_selectedCustomer != null)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Pliki Dukumentów (*.pdf)|*.pdf|Wszystkie Pliki (*.*)|*.*";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            dialog.FileName = $"swiadectwo_{_selectedCustomer.Name}_{_selectedCustomer.Surname}.pdf";

            string path = "";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                path = dialog.FileName;
                _pdfService.GeneratePdf(_selectedCustomer, path);
            }
        }
        else
        {
            MessageBox.Show("Proszę wybrać dane zmarłego aby móc wygenerować plik PDF", "Nie wybrano danych",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void UpdateCustomer()
    {
        if (_selectedCustomer != null)
        {
            _customerService.UpdateCustomer(_selectedCustomer);
        }
        else
        {
            MessageBox.Show("Proszę wybrać dane zmarłego aby móc dokonać w nich zmian.", "Nie wybrano danych",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}