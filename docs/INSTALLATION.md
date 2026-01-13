# 🔧 Installation Guide / Guía de Instalación

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="#instalación-español">🇪🇸 Español</a>
</p>

---

## Installation (English)

### Option 1: Portable Version (Recommended)

The portable version is self-contained and requires no installation.

1. **Download** the latest release:
   - Go to [Releases](../../releases)
   - Download `RTSPVirtualCam-v1.0.0-portable-win-x64.zip`

2. **Extract** the ZIP file to any location

3. **Run** `RTSPVirtualCam.exe`

That's it! No installation, no registry changes, no admin rights needed.

### Option 2: Build from Source

#### Prerequisites

| Software | Version | Download |
|----------|---------|----------|
| .NET SDK | 8.0+ | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Git | Any | [Download](https://git-scm.com/downloads) |
| Visual Studio (optional) | 2022+ | [Download](https://visualstudio.microsoft.com/) |

#### Build Steps

```powershell
# 1. Clone the repository
git clone https://github.com/YOUR_USERNAME/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# 2. Restore NuGet packages
dotnet restore

# 3. Build the project
dotnet build -c Release

# 4. Run the application
dotnet run --project src/RTSPVirtualCam -c Release
```

#### Create Portable Executable

```powershell
# Create single-file self-contained executable
dotnet publish src/RTSPVirtualCam `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o ./publish

# Your portable exe is now at: ./publish/RTSPVirtualCam.exe
```

### System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Windows 11 Build 22000 | Windows 11 22H2+ |
| RAM | 2 GB | 4 GB |
| CPU | Any x64 | Multi-core |
| Network | 10 Mbps | 100 Mbps |

---

## Instalación (Español)

### Opción 1: Versión Portable (Recomendada)

La versión portable es auto-contenida y no requiere instalación.

1. **Descarga** la última versión:
   - Ve a [Releases](../../releases)
   - Descarga `RTSPVirtualCam-v1.0.0-portable-win-x64.zip`

2. **Extrae** el archivo ZIP en cualquier ubicación

3. **Ejecuta** `RTSPVirtualCam.exe`

¡Eso es todo! Sin instalación, sin cambios en el registro, sin permisos de administrador.

### Opción 2: Compilar desde Código Fuente

#### Prerrequisitos

| Software | Versión | Descarga |
|----------|---------|----------|
| .NET SDK | 8.0+ | [Descargar](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Git | Cualquiera | [Descargar](https://git-scm.com/downloads) |
| Visual Studio (opcional) | 2022+ | [Descargar](https://visualstudio.microsoft.com/) |

#### Pasos de Compilación

```powershell
# 1. Clonar el repositorio
git clone https://github.com/YOUR_USERNAME/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Compilar el proyecto
dotnet build -c Release

# 4. Ejecutar la aplicación
dotnet run --project src/RTSPVirtualCam -c Release
```

#### Crear Ejecutable Portable

```powershell
# Crear ejecutable auto-contenido de archivo único
dotnet publish src/RTSPVirtualCam `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o ./publish

# Tu exe portable está ahora en: ./publish/RTSPVirtualCam.exe
```

### Requisitos del Sistema

| Componente | Mínimo | Recomendado |
|------------|--------|-------------|
| SO | Windows 11 Build 22000 | Windows 11 22H2+ |
| RAM | 2 GB | 4 GB |
| CPU | Cualquier x64 | Multi-núcleo |
| Red | 10 Mbps | 100 Mbps |
