# Who's Singing

Aplicación web interactiva desarrollada con ASP.NET Core 8 que permite identificar canciones y artistas en tiempo real. Combina tecnologías modernas de desarrollo web con comunicación en tiempo real mediante SignalR, creando una experiencia de juego colaborativa y dinámica.

El sistema permite la gestión de canciones mediante un script automatizado de Python y ofrece una interfaz responsiva para múltiples usuarios conectados simultáneamente.

## Características principales

- Comunicación en tiempo real mediante SignalR
- Gestión de usuarios conectados simultáneamente
- Reproducción de audio integrada
- Sistema de puntuación en tiempo real
- Interfaz responsiva y moderna
- Descarga automatizada de contenido mediante Python
- Arquitectura MVC con separación clara de responsabilidades
- Acceso remoto seguro mediante Cloudflare Tunnel

## Tecnologías utilizadas

### Backend
- ASP.NET Core 8 MVC
- SignalR para comunicación en tiempo real
- Entity Framework Core
- SQLite como base de datos local (desarrollo)

### Frontend
- HTML5, CSS3, JavaScript
- SignalR Client para JavaScript
- Web Audio API

### Herramientas y servicios
- Python para automatización de descarga de canciones
- Cloudflare Tunnel para exposición segura
- Visual Studio 2022

### Stack de despliegue (planificado)
- MongoDB Atlas como base de datos en la nube
- Render como plataforma de hosting
- Cloudinary para almacenamiento de archivos de audio

## Arquitectura del proyecto

El proyecto sigue el patrón de diseño MVC con una estructura organizada en capas:

- Controllers: Manejo de rutas y lógica de presentación
- Models: Entidades de dominio y modelos de vista
- Views: Interfaces de usuario con Razor
- Services: Lógica de negocio y servicios de aplicación
- Hubs: Hubs de SignalR para comunicación en tiempo real
- Data: Contexto de base de datos y migraciones

## Requisitos previos

- .NET 8 SDK o superior
- Visual Studio 2022 o VS Code
- Python 3.8 o superior (para el script de descarga de canciones)
- Git

## Instalación

Clonar el repositorio:
git clone https://github.com/tu-usuario/WhosSinging.git


Navegar al directorio del proyecto:
cd WhosSinging/WhosSinging.Web
Restaurar las dependencias de NuGet:
dotnet restore
Configuración
Base de datos
El proyecto utiliza SQLite por defecto para desarrollo local. La base de datos se crea automáticamente al ejecutar la aplicación por primera vez.
Para recrear la base de datos:
dotnet ef database update
Archivos de audio
Los archivos de audio deben colocarse en el directorio configurado en appsettings.json. Por defecto:
dotnet ef database update
Se puede modificar esta ruta en el archivo de configuración según la estructura de directorios local.
Descarga de canciones
El proyecto incluye un script de Python para descargar canciones automáticamente:
cd scripts
python download_songs.py
Ejecución del proyecto
Desarrollo local
Ejecutar la aplicación con:
dotnet run
La aplicación estará disponible en:
https://localhost:5001
Acceso remoto con Cloudflare Tunnel
El proyecto incluye un script para levantar tanto la aplicación como el túnel de Cloudflare:
start.bat
Este script ejecuta simultáneamente:
La aplicación ASP.NET Core
El túnel de Cloudflare para acceso público seguro
Estructura del proyecto

WhosSinging/
├── WhosSinging.Web/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/
│   ├── Hubs/
│   ├── Data/
│   ├── wwwroot/
│   ├── appsettings.json
│   └── Program.cs
├── scripts/
│   └── download_songs.py
└── README.md

Roadmap
Migración de SQLite a MongoDB Atlas para despliegue en la nube
Integración con Cloudinary para almacenamiento de archivos de audio
Despliegue en Render con URL pública
Sistema de autenticación de usuarios
Leaderboard global de puntuaciones
Modo competitivo entre usuarios
Implementación de IA para generación de pistas
Autor
Desarrollado como parte del proceso de formación en tecnicatura de programación, con enfoque en tecnologías modernas de desarrollo web y arquitectura de software.
Licencia
Este proyecto está bajo la Licencia MIT.
