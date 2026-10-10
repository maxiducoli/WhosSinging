Who's Singing
Descripción
Who's Singing es una aplicación web interactiva desarrollada con ASP.NET Core 8 que permite a los usuarios identificar canciones y artistas en tiempo real. El proyecto combina tecnologías modernas de desarrollo web con comunicación en tiempo real mediante SignalR, creando una experiencia de juego colaborativa y dinámica.
El sistema permite la gestión de canciones mediante un script automatizado de Python y ofrece una interfaz responsiva para múltiples usuarios conectados simultáneamente.
Características principales
Comunicación en tiempo real mediante SignalR
Gestión de usuarios conectados simultáneamente
Reproducción de audio integrada
Sistema de puntuación en tiempo real
Interfaz responsiva y moderna
Descarga automatizada de contenido mediante Python
Arquitectura MVC con separación clara de responsabilidades
Acceso remoto seguro mediante Cloudflare Tunnel
Tecnologías utilizadas
Backend:
ASP.NET Core 8 MVC
SignalR para comunicación en tiempo real
Entity Framework Core
SQLite como base de datos local
Frontend:
HTML5, CSS3, JavaScript
SignalR Client para JavaScript
Web Audio API
Herramientas y servicios:
Python para automatización de descarga de canciones
Cloudflare Tunnel para exposición segura
Visual Studio 2022
Arquitectura del proyecto
El proyecto sigue el patrón de diseño MVC (Model-View-Controller) con una estructura organizada en capas:
Controllers: Manejo de rutas y lógica de presentación
Models: Entidades de dominio y modelos de vista
Views: Interfaces de usuario con Razor
Services: Lógica de negocio y servicios de aplicación
Hubs: Hubs de SignalR para comunicación en tiempo real
Data: Contexto de base de datos y migraciones
Requisitos previos
Antes de comenzar, asegúrate de tener instalado:
.NET 8 SDK o superior
Visual Studio 2022 o VS Code
Python 3.8 o superior (para el script de descarga de canciones)
Git
Instalación
Clona el repositorio:
1
Navega al directorio del proyecto:
1
Restaura las dependencias de NuGet:
1
Configuración
Base de datos
El proyecto utiliza SQLite por defecto. La base de datos se crea automáticamente al ejecutar la aplicación por primera vez.
Si necesitas recrear la base de datos:
1
Archivos de audio
Los archivos de audio deben colocarse en el directorio configurado en appsettings.json. Por defecto:
1
Puedes modificar esta ruta en el archivo de configuración según tu estructura de directorios.
Descarga de canciones
El proyecto incluye un script de Python para descargar canciones automáticamente. Para usarlo:
12
Ejecución del proyecto
Desarrollo local
Ejecuta la aplicación con:

La aplicación estará disponible en:

Acceso remoto con Cloudflare Tunnel
El proyecto incluye un script para levantar tanto la aplicación como el túnel de Cloudflare:

Este script ejecuta simultáneamente:
La aplicación ASP.NET Core
El túnel de Cloudflare para acceso público seguro
Estructura del proyecto
1234567891011121314
Próximas mejoras
Migración a base de datos PostgreSQL para despliegue en la nube
Integración con servicios de almacenamiento en la nube para archivos de audio
Sistema de autenticación de usuarios
Leaderboard global de puntuaciones
Modo competitivo entre usuarios
Implementación de IA para generación de pistas
Autor
Desarrollado como parte del proceso de formación en tecnicatura de programación, con enfoque en tecnologías modernas de desarrollo web y arquitectura de software.
Licencia
Este proyecto está bajo la Licencia MIT. Consulta el archivo LICENSE para más detalles.
Para más información sobre el desarrollo y las decisiones técnicas, consulta la documentación del proyecto o contacta al autor a través de GitHub.
