# 🛠 Development Guide

<div align="center">

**Guía de Desarrollo para RTSP VirtualCam**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-0078D4?style=for-the-badge&logo=windows)](https://github.com/dotnet/wpf)

</div>

---

## 📁 Estructura del Proyecto

```
RTSPVirtualCam/
├── 📂 src/RTSPVirtualCam/              # Aplicación principal WPF
│   ├── 📂 Models/                      # Modelos de datos
│   │   ├── ConnectionInfo.cs           # Información de conexión RTSP
│   │   ├── CameraSettings.cs           # Configuración de cámara
│   │   └── AppSettings.cs              # Configuración de aplicación
│   │
│   ├── 📂 Services/                    # Lógica de negocio principal
│   │   ├── IRtspService.cs             # Interfaz servicio RTSP
│   │   ├── RtspService.cs              # Implementación RTSP con LibVLC
│   │   ├── IVirtualCameraService.cs    # Interfaz cámara virtual
│   │   └── VirtualCameraService.cs     # Implementación cámara virtual
│   │
│   ├── 📂 ViewModels/                  # MVVM ViewModels
│   │   └── MainViewModel.cs            # ViewModel principal
│   │
│   ├── 📂 Views/                       # Vistas XAML
│   │   ├── MainWindow.xaml             # Ventana principal
│   │   └── MainWindow.xaml.cs          # Code-behind
│   │
│   ├── 📂 Core/                        # Funcionalidad core
│   │   ├── RtspClient.cs               # Cliente RTSP
│   │   ├── FrameBuffer.cs              # Buffer de frames
│   │   └── FrameProcessor.cs           # Procesamiento de video
│   │
│   ├── 📂 Helpers/                     # Utilidades
│   │   ├── Converters.cs               # Convertidores XAML
│   │   └── DiagnosticLogger.cs         # Logging diagnóstico
│   │
│   ├── App.xaml                        # Configuración aplicación
│   ├── App.xaml.cs                     # Entry point
│   └── RTSPVirtualCam.csproj           # Proyecto .NET
│
├── 📂 docs/                            # Documentación
│   ├── README_ES.md                    # Documentación principal
│   ├── USER_GUIDE.md                   # Guía de usuario
│   ├── DEVELOPMENT.md                  # Guía de desarrollo
│   ├── TROUBLESHOOTING.md              # Solución de problemas
│   ├── Specification.md                # Especificación técnica
│   ├── Implementation_Guide.md         # Guía de implementación
│   └── INSTALLATION.md                 # Guía de instalación
│
├── 📂 scripts/                         # Scripts de utilidad
│   ├── create-release.ps1              # Crear release
│   ├── download-hikvision-sdk.ps1      # Descargar SDK Hikvision
│   ├── install-unity-multicam.bat      # Instalar Unity multicam
│   └── install-virtualcam.bat          # Instalar cámara virtual
│
├── 📂 releases/                        # Versiones compiladas
│   └── RTSPVirtualCam-v1.0.0-portable-win-x64/
│
├── 📄 RTSPVirtualCam.sln               # Solución Visual Studio
├── 📄 README.md                        # README principal
├── 📄 LICENSE                          # Licencia
└── 📄 .gitignore                       # Reglas Git
```

---

## 🔧 Stack Tecnológico

| Componente | Tecnología | Versión | Propósito |
|------------|------------|---------|-----------|
| **Runtime** | .NET | 8.0+ | Framework principal |
| **UI Framework** | WPF | - | Interfaz de usuario nativa Windows |
| **RTSP Streaming** | LibVLCSharp | 3.8.5 | Decodificación y streaming RTSP |
| **Virtual Camera** | DirectN | 2024.6.26.1 | API MFCreateVirtualCamera |
| **MVVM Pattern** | CommunityToolkit.Mvvm | 8.2.2 | Implementación MVVM |
| **Logging** | Serilog | 4.0.0 | Registro estructurado |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection | 8.0.0 | Contenedor DI |
| **JSON Handling** | System.Text.Json | Built-in | Configuración y persistencia |

---

## 🚀 Configuración del Entorno

### Prerrequisitos
- **Windows 11 Build 22000+** (para cámara virtual)
- **.NET 8 SDK** (para desarrollo)
- **Visual Studio 2022** o **VS Code** (opcional)
- **Git** (control de versiones)

### Instalación
```bash
# Clonar repositorio
git clone https://github.com/RaulJuliosIglesias/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restaurar paquetes NuGet
dotnet restore

# Compilar proyecto
dotnet build -c Release

# Ejecutar aplicación
dotnet run --project src/RTSPVirtualCam -c Release
```

---

## 🏗️ Arquitectura

### Patrón MVVM
```
┌─────────────────────────────────────────────────────────────┐
│                    MVVM Architecture                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │    View     │◄──▶│  ViewModel    │◄──▶│    Model     │ │
│  │ (MainWindow)│    │(MainViewModel)│    │(ConnectionInfo)│ │
│  └─────────────┘    └──────────────┘    └──────────────┘ │
│                                                             │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │                   Services Layer                        │ │
│  │  ┌──────────────┐  ┌─────────────────────────────────┐ │ │
│  │  │ RtspService  │  │    VirtualCameraService         │ │ │
│  │  └──────────────┘  └─────────────────────────────────┘ │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Flujo de Datos
1. **View** → **ViewModel**: User actions via commands
2. **ViewModel** → **Services**: Business logic execution
3. **Services** → **ViewModel**: Status updates via events
4. **ViewModel** → **View**: Property updates via INotifyPropertyChanged

---

## 🔨 Build & Deployment

### Development Build
```bash
# Build debug
dotnet build src/RTSPVirtualCam -c Debug

# Run debug
dotnet run --project src/RTSPVirtualCam -c Debug
```

### Release Build
```bash
# Build release
dotnet build src/RTSPVirtualCam -c Release

# Create portable executable
dotnet publish src/RTSPVirtualCam `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o ./publish
```

### Package Creation
```powershell
# Create release package
./scripts/create-release.ps1

# Output: releases/RTSPVirtualCam-v1.0.0-portable-win-x64.zip
```

---

## 🧪 Testing

### Unit Tests (Planeados)
```bash
# Run unit tests
dotnet test src/RTSPVirtualCam.Tests

# Coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Integration Testing
- **RTSP Connection Tests**: Verify connection to various camera brands
- **Virtual Camera Tests**: Test camera creation and frame delivery
- **UI Tests**: Verify user interaction flows

---

## 🔍 Debugging

### Logging Configuration
```csharp
// App.xaml.cs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/rtspvirtualcam.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();
```

### Common Debug Points
- **RTSP Connection**: Check VLC logs for stream errors
- **Virtual Camera**: Verify Windows 11 build and permissions
- **UI Binding**: Check DataContext and property notifications

### Debug Tools
- **Visual Studio Debugger**: Breakpoints and watch windows
- **Serilog Console**: Real-time logging during development
- **Windows Event Viewer**: System-level camera API errors

---

## 📝 Code Style & Conventions

### C# Conventions
- **PascalCase**: Classes, methods, properties
- **camelCase**: Local variables, parameters
- **_underscore**: Private fields
- **async/await**: Async operations
- **nullable reference types**: Enabled (`string?`, `Class?`)

### XAML Conventions
- **x:Name**: PascalCase for elements
- **Bindings**: Two-way for user input, One-way for display
- **Commands**: Use CommunityToolkit.Mvvm [RelayCommand]
- **Resources**: Organized in separate ResourceDictionary files

### File Organization
- **One class per file** (except nested classes)
- **Folder structure** matches namespace hierarchy
- **Interface naming**: `I` prefix (e.g., `IRtspService`)

---

## 🚀 Contributing

### Development Workflow
1. **Create feature branch**: `git checkout -b feature/new-feature`
2. **Implement changes**: Follow code style and patterns
3. **Test thoroughly**: Unit tests and manual testing
4. **Update documentation**: Keep docs in sync
5. **Submit PR**: With clear description and testing notes

### Code Review Checklist
- [ ] Follows MVVM pattern correctly
- [ ] Proper error handling and logging
- [ ] No hardcoded values (use configuration)
- [ ] Async operations properly handled
- [ ] UI bindings are correct
- [ ] Documentation updated

---

## 🔮 Future Development

### Planned Features
- **Settings Window**: Advanced configuration UI
- **Multiple Cameras**: Support for simultaneous streams
- **PTZ Control**: Camera movement controls
- **Recording**: Stream recording functionality
- **System Tray**: Minimize to tray support

### Technical Improvements
- **Hardware Acceleration**: DXVA2 for video processing
- **Better Error Handling**: Retry mechanisms and fallbacks
- **Performance Optimization**: Memory management and threading
- **Windows 10 Support**: DirectShow fallback implementation

---

<div align="center">

**© 2026 Raúl Julios Iglesias - Todos los Derechos Reservados**

</div>
