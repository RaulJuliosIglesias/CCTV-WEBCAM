# 🚀 Quick Start: Deploy to GitHub (5 minutos)

Guía rápida para configurar tu repositorio y empezar a distribuir releases automáticamente.

## ¿Qué conseguirás?

- ✅ Código fuente **PRIVADO** (solo tú lo ves)
- ✅ Releases **PÚBLICAS** (cualquiera las descarga)
- ✅ Builds automáticos con cada tag
- ✅ Checksums de seguridad incluidos

## Opción Recomendada: Repo Privado + Workflow Automático

### Paso 1: Hacer el Repositorio Privado (30 segundos)

```bash
# En GitHub:
# 1. Ve a Settings (Configuración)
# 2. Baja hasta "Danger Zone" 
# 3. Click en "Change visibility" → "Make private"
# 4. Confirma escribiendo el nombre del repo
```

### Paso 2: Verificar el Workflow (ya está listo ✅)

El workflow ya está configurado en `.github/workflows/build.yml`

**¿Qué hace automáticamente?**
- Compila el proyecto
- Crea el portable con todas las dependencias
- Genera checksums SHA256
- Crea GitHub Release con los archivos
- Extrae el changelog de CHANGELOG.md

### Paso 3: Crear tu Primera Release (1 minuto)

```bash
# Desde tu terminal:
git tag -a v1.0.0 -m "Primera release"
git push origin v1.0.0
```

**Eso es todo.** GitHub Actions hará el resto.

### Paso 4: Compartir el Link de Descarga

Después de que se complete el workflow (2-3 minutos):

```
https://github.com/TU_USUARIO/RTSPVirtualCam/releases/download/v1.0.0/RTSPVirtualCam-v1.0.0-portable-win-x64.zip
```

**Nota**: Aunque el repo sea privado, puedes compartir este link directo para descargas.

---

## Alternativa: Repo Público de Distribución

Si prefieres tener un repo público solo para descargas:

### Setup Rápido

1. **Crea un nuevo repo público**
   ```
   Nombre: RTSPVirtualCam-Downloads
   Descripción: Download RTSP VirtualCam releases
   Público: ✅
   ```

2. **Genera un Personal Access Token**
   ```
   GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
   Scopes: ✅ repo (full control)
   ```

3. **Añade el token a tu repo privado**
   ```
   Repo Privado → Settings → Secrets and variables → Actions
   New repository secret:
   Name: RELEASE_REPO_TOKEN
   Value: [tu_token_aquí]
   ```

4. **Crea el workflow de deploy** (ver `GITHUB_SETUP.md` para el código completo)

---

## Configuración Recomendada por Caso

### Si quieres máxima simplicidad:
**→ Repo Privado + Links Directos**
- Solo hacer el repo privado
- Compartir links de descarga directos
- Ya está configurado ✅

### Si quieres página de descargas profesional:
**→ Repo Privado + Repo Público de Distribución**
- Requiere configuración adicional
- Página pública de descargas
- Ver `GITHUB_SETUP.md` para detalles

### Si no te importa que vean el código:
**→ Repo Público con Releases**
- No hacer nada, ya funciona
- Todo público y visible

---

## Comandos Útiles

### Crear Release

```bash
# Tag y release automático
git tag -a v1.2.3 -m "Release v1.2.3"
git push origin v1.2.3
```

### Release Manual (sin tag)

```bash
# Ir a Actions en GitHub
# Click en "🚀 Build and Release"
# Click "Run workflow"
# Ingresar versión: 1.2.3
```

### Ver Releases

```bash
# Con GitHub CLI
gh release list
gh release view v1.0.0
gh release download v1.0.0
```

### Build Local (para probar antes)

```powershell
.\scripts\create-release.ps1 -Version "1.0.0"
```

---

## Checklist Antes de Cada Release

- [ ] Actualizar `CHANGELOG.md` con cambios de la nueva versión
- [ ] Probar build local: `.\scripts\create-release.ps1 -Version "X.Y.Z"`
- [ ] Verificar que funciona el ejecutable generado
- [ ] Crear tag: `git tag -a vX.Y.Z -m "Release vX.Y.Z"`
- [ ] Push tag: `git push origin vX.Y.Z`
- [ ] Esperar a que termine el workflow (ver Actions)
- [ ] Verificar que la release aparece en GitHub
- [ ] Probar descarga y checksum

---

## Troubleshooting Rápido

### El workflow no se ejecuta

**Problema**: Pusheaste un tag pero no pasa nada

**Solución**: 
```bash
# Verifica el formato del tag
git tag -l  # Debe ser v1.2.3 (con 'v')

# Asegúrate de que el tag se subió
git ls-remote --tags origin
```

### La release está vacía

**Problema**: Se crea la release pero sin archivos

**Solución**: Ve a Actions → Revisa el log del job "build"
- Busca errores en la compilación
- Verifica que `dotnet publish` completó exitosamente

### No puedo hacer el repo privado

**Problema**: GitHub no permite cambiar visibilidad

**Solución**: 
- Asegúrate de tener permisos de admin en el repo
- Si es una organización, verifica los permisos de la org

---

## Links de Documentación Completa

- **[DEPLOYMENT.md](docs/DEPLOYMENT.md)** - Guía completa de deployment
- **[GITHUB_SETUP.md](docs/GITHUB_SETUP.md)** - Configuración detallada de GitHub
- **[DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Guía de desarrollo

---

## Próximos Pasos

Después de tu primera release exitosa:

1. ✅ Configura [branch protection](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
2. ✅ Añade [code signing](https://learn.microsoft.com/en-us/windows/win32/seccrypto/cryptography-tools) para releases de producción
3. ✅ Configura [dependabot](https://docs.github.com/en/code-security/dependabot) para actualizaciones de seguridad
4. ✅ Añade badges al README: build status, latest release, downloads

---

**¿Dudas?** Revisa las guías completas o abre un issue.
