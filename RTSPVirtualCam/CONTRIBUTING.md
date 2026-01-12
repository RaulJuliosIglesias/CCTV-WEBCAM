# 🤝 Contributing / Contribuir

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="#contribuir-español">🇪🇸 Español</a>
</p>

---

## Contributing (English)

First off, thank you for considering contributing to RTSP VirtualCam! 🎉

### How Can I Contribute?

#### 🐛 Reporting Bugs

Before creating bug reports, please check existing issues. When you create a bug report, include:

- **Clear title** describing the issue
- **Steps to reproduce** the behavior
- **Expected behavior** vs what actually happened
- **Screenshots** if applicable
- **Environment info** (Windows version, .NET version)
- **Log files** from `./logs/`

#### 💡 Suggesting Features

Feature requests are welcome! Please include:

- **Clear description** of the feature
- **Use case** - why is this feature useful?
- **Possible implementation** if you have ideas

#### 🔧 Pull Requests

1. Fork the repo
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Run tests: `dotnet test`
5. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

### Development Setup

```powershell
# Clone your fork
git clone https://github.com/YOUR_USERNAME/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/RTSPVirtualCam
```

### Code Style

- Use C# conventions
- Add XML documentation for public APIs
- Keep methods small and focused
- Write meaningful commit messages

### Commit Message Format

```
type(scope): description

[optional body]

[optional footer]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Example:
```
feat(rtsp): add auto-reconnect on connection loss

- Implement exponential backoff
- Add max retry attempts configuration
- Update UI to show reconnection status
```

---

## Contribuir (Español)

¡Primero que nada, gracias por considerar contribuir a RTSP VirtualCam! 🎉

### ¿Cómo Puedo Contribuir?

#### 🐛 Reportar Bugs

Antes de crear reportes de bugs, por favor revisa los issues existentes. Cuando crees un reporte de bug, incluye:

- **Título claro** describiendo el problema
- **Pasos para reproducir** el comportamiento
- **Comportamiento esperado** vs lo que realmente sucedió
- **Capturas de pantalla** si aplica
- **Información del entorno** (versión de Windows, versión de .NET)
- **Archivos de log** de `./logs/`

#### 💡 Sugerir Características

¡Las solicitudes de características son bienvenidas! Por favor incluye:

- **Descripción clara** de la característica
- **Caso de uso** - ¿por qué es útil esta característica?
- **Posible implementación** si tienes ideas

#### 🔧 Pull Requests

1. Haz fork del repo
2. Crea tu rama de feature (`git checkout -b feature/CaracteristicaIncreible`)
3. Haz tus cambios
4. Ejecuta tests: `dotnet test`
5. Haz commit de tus cambios (`git commit -m 'Agrega CaracteristicaIncreible'`)
6. Push a la rama (`git push origin feature/CaracteristicaIncreible`)
7. Abre un Pull Request

### Configuración de Desarrollo

```powershell
# Clona tu fork
git clone https://github.com/TU_USUARIO/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restaura dependencias
dotnet restore

# Compila
dotnet build

# Ejecuta
dotnet run --project src/RTSPVirtualCam
```

### Estilo de Código

- Usa convenciones de C#
- Agrega documentación XML para APIs públicas
- Mantén los métodos pequeños y enfocados
- Escribe mensajes de commit significativos

### Formato de Mensaje de Commit

```
tipo(alcance): descripción

[cuerpo opcional]

[pie opcional]
```

Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Ejemplo:
```
feat(rtsp): agrega auto-reconexión en pérdida de conexión

- Implementa backoff exponencial
- Agrega configuración de intentos máximos
- Actualiza UI para mostrar estado de reconexión
```
