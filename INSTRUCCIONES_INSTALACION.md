# 📦 Instrucciones de Instalación - GitHub Desktop App

## ⚠️ Requisito Previo: Instalar .NET SDK

Para ejecutar esta aplicación, primero necesitas instalar el .NET SDK.

### Paso 1: Descargar e Instalar .NET SDK

1. **Visita la página oficial de Microsoft**:
   - Ve a: https://dotnet.microsoft.com/download
   - O directamente: https://aka.ms/dotnet/download

2. **Descarga .NET 8.0 SDK** (o superior)
   - Haz clic en el botón "Download .NET SDK"
   - Selecciona la versión para Windows x64

3. **Ejecuta el instalador**
   - Abre el archivo `.exe` descargado
   - Sigue las instrucciones del instalador
   - El proceso toma aproximadamente 2-3 minutos

4. **Verifica la instalación**
   - Abre una nueva terminal (PowerShell o CMD)
   - Ejecuta: `dotnet --version`
   - Deberías ver algo como: `8.0.xxx`

### Paso 2: Compilar y Ejecutar la Aplicación

Una vez instalado el .NET SDK, sigue estos pasos:

#### Opción 1: Ejecutar en modo desarrollo

Abre PowerShell o CMD en la carpeta del proyecto y ejecuta:

```powershell
# Cambiar al directorio del proyecto
cd "C:\Users\alvar\Desktop\GitHubDesktopApp"

# Restaurar dependencias (solo la primera vez)
dotnet restore

# Compilar el proyecto
dotnet build

# Ejecutar la aplicación
dotnet run
```

#### Opción 2: Crear ejecutable (.exe)

Para crear un archivo ejecutable que puedas usar sin comandos:

```powershell
# Cambiar al directorio del proyecto
cd "C:\Users\alvar\Desktop\GitHubDesktopApp"

# Publicar la aplicación
dotnet publish -c Release -r win-x64 --self-contained false
```

El archivo `.exe` estará en:
```
bin\Release\net8.0-windows\win-x64\publish\GitHubDesktopApp.exe
```

**Nota**: Si usas `--self-contained true`, el ejecutable incluirá todo el runtime de .NET (será más grande, ~100MB, pero no necesitará .NET instalado para ejecutarse).

#### Opción 3: Desde Visual Studio 2022

Si tienes Visual Studio instalado:

1. Abre el archivo `GitHubDesktopApp.csproj` con Visual Studio
2. Presiona F5 o haz clic en "Iniciar" (botón verde)
3. La aplicación se compilará y ejecutará automáticamente

### Paso 3: Primer Uso

1. **Ejecuta la aplicación**
2. **Prueba con un repositorio público**, por ejemplo:
   ```
   https://github.com/microsoft/vscode
   ```
3. **Selecciona "Commits"** en el menú desplegable
4. **Haz clic en "Ejecutar"**
5. Verás la lista de commits del repositorio

### 🔑 Opcional: Configurar Token de GitHub

Para evitar límites de la API (60 requests/hora sin token):

1. Ve a GitHub: https://github.com/settings/tokens
2. Click en "Generate new token" → "Generate new token (classic)"
3. Dale un nombre descriptivo (ej: "Desktop App")
4. Selecciona el permiso: `public_repo` (para repositorios públicos)
5. O `repo` (para repositorios privados también)
6. Click en "Generate token"
7. **COPIA EL TOKEN** (comienza con `ghp_`)
8. Pégalo en la aplicación en el campo "Token de GitHub"

Con token, el límite aumenta a **5000 requests/hora**.

## 🎯 Ejemplos de URLs para Probar

### Repositorios Populares:
```
https://github.com/microsoft/vscode
https://github.com/dotnet/aspnetcore
https://github.com/facebook/react
https://github.com/nodejs/node
```

### Con Directorios Específicos:
```
https://github.com/microsoft/vscode/tree/main/src
https://github.com/dotnet/aspnetcore/tree/main/src
```

## ❓ Solución de Problemas

### Problema 1: "dotnet no se reconoce como un comando"
**Causa**: .NET SDK no está instalado o la variable PATH no está actualizada.

**Solución**:
1. Instala .NET SDK (ver Paso 1)
2. Cierra y abre nuevamente la terminal
3. Si persiste, reinicia el ordenador

### Problema 2: "The type or namespace name 'Octokit' could not be found"
**Causa**: Las dependencias NuGet no se han restaurado.

**Solución**:
```powershell
dotnet restore
```

### Problema 3: La aplicación no muestra nada
**Causa**: Probablemente límite de la API alcanzado.

**Solución**:
1. Espera 1 hora
2. O usa un token de GitHub

### Problema 4: "Rate limit exceeded"
**Causa**: Excediste el límite de requests de la API.

**Solución**:
- Agrega un token de GitHub en la aplicación
- O espera a que se reinicie el límite (cada hora)

## 📞 Soporte Adicional

Si encuentras algún problema:
1. Verifica que .NET SDK esté instalado: `dotnet --version`
2. Revisa el archivo `README.md` para más información
3. Consulta la documentación de .NET: https://docs.microsoft.com/dotnet

---

**¡Listo! Ya puedes usar la aplicación para conectarte a GitHub desde tu escritorio.** 🚀
