using System;
using System.ComponentModel;

namespace RTSPVirtualCam.Models;

public class CameraProfile : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _ipAddress = string.Empty;
    private int _port = 554;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _ptzUsername = string.Empty;
    private string _ptzPassword = string.Empty;
    private CameraBrand _brand = CameraBrand.Hikvision;
    private StreamType _stream = StreamType.MainStream;
    private int _channel = 1;
    private bool _useManualUrl = false;
    private string _manualUrl = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string IpAddress
    {
        get => _ipAddress;
        set
        {
            _ipAddress = value;
            OnPropertyChanged(nameof(IpAddress));
        }
    }

    public int Port
    {
        get => _port;
        set
        {
            _port = value;
            OnPropertyChanged(nameof(Port));
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged(nameof(Username));
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged(nameof(Password));
        }
    }

    public string PtzUsername
    {
        get => _ptzUsername;
        set
        {
            _ptzUsername = value;
            OnPropertyChanged(nameof(PtzUsername));
        }
    }

    public string PtzPassword
    {
        get => _ptzPassword;
        set
        {
            _ptzPassword = value;
            OnPropertyChanged(nameof(PtzPassword));
        }
    }

    public CameraBrand Brand
    {
        get => _brand;
        set
        {
            _brand = value;
            OnPropertyChanged(nameof(Brand));
        }
    }

    public StreamType Stream
    {
        get => _stream;
        set
        {
            _stream = value;
            OnPropertyChanged(nameof(Stream));
        }
    }

    public int Channel
    {
        get => _channel;
        set
        {
            _channel = value;
            OnPropertyChanged(nameof(Channel));
        }
    }

    public bool UseManualUrl
    {
        get => _useManualUrl;
        set
        {
            _useManualUrl = value;
            OnPropertyChanged(nameof(UseManualUrl));
        }
    }

    public string ManualUrl
    {
        get => _manualUrl;
        set
        {
            _manualUrl = value;
            OnPropertyChanged(nameof(ManualUrl));
        }
    }

    public string DisplayName => string.IsNullOrEmpty(Name) ? $"Camera {IpAddress}" : Name;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public CameraProfile Clone()
    {
        return new CameraProfile
        {
            Id = Id,
            Name = Name,
            IpAddress = IpAddress,
            Port = Port,
            Username = Username,
            Password = Password,
            PtzUsername = PtzUsername,
            PtzPassword = PtzPassword,
            Brand = Brand,
            Stream = Stream,
            Channel = Channel,
            UseManualUrl = UseManualUrl,
            ManualUrl = ManualUrl
        };
    }
}
