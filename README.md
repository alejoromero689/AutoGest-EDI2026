# AutoGest · Gestión automotor

Aplicación del proyecto integrador EDI 2026, desarrollada con **Blazor Server sobre .NET 8**, **Bootstrap 5**, **Entity Framework Core** y **MySQL 8.4**. Integra el catálogo original de vehículos con clientes, compras, ventas y stock.

## Funcionalidades

- **Inicio:** indicadores reales del negocio, accesos rápidos y últimas ventas.
- **Menú:** navegación lateral con sección activa y menú desplegable en celular.
- **Vehículos:** conserva alta, edición, baja, búsqueda, paginación y validaciones del proyecto original.
- **Clientes:** CRUD, búsqueda por nombre/documento/correo, paginación, documento único y validación de contacto. Un mismo contacto puede ser comprador y proveedor.
- **Compras y ventas:** CRUD, búsqueda, paginación, fechas e importes validados, relaciones con cliente y vehículo y control de edición simultánea.
- **Stock:** disponibilidad calculada desde las ventas, costo de compra, precio de catálogo, filtros, paginación y descarga CSV de la página visible mediante JavaScript Interop.

Los importes se muestran en **pesos argentinos (ARS)**. Los formularios monetarios de operaciones usan el formato `12.500.000,00`.

## Requisitos

- .NET SDK **8.0.400 o posterior de la rama 8.0** (`global.json` permite `latestFeature`).
- MySQL **8.4**; el proveedor Pomelo está configurado para esa versión.
- Visual Studio 2022 actualizado con la carga **ASP.NET y desarrollo web**, o una terminal.
- Docker, únicamente si se elige el inicio con contenedores.

## Inicio rápido con MySQL local

1. Abrí `AutoGest/AutoGest.sln` o ubicá la terminal en la raíz del repositorio.
2. En MySQL, creá una base y un usuario de desarrollo. Si ya tenés la base del proyecto, conservá su nombre y su historial de migraciones:

```sql
CREATE DATABASE IF NOT EXISTS autogest CHARACTER SET utf8mb4;
CREATE USER IF NOT EXISTS 'autogest'@'localhost' IDENTIFIED BY 'TU_CLAVE_LOCAL';
GRANT ALL PRIVILEGES ON autogest.* TO 'autogest'@'localhost';
```

3. Configurá la conexión mediante **User Secrets**, sin guardar tu contraseña en Git:

```sh
dotnet user-secrets set "ConnectionStrings:GestoriaDb" "Server=localhost;Port=3306;Database=autogest;User=autogest;Password=TU_CLAVE_LOCAL" --project AutoGest/AutoGest
```

4. Restaurá, aplicá las migraciones y ejecutá:

```sh
dotnet tool restore
dotnet restore AutoGest/AutoGest.sln
dotnet ef database update --project AutoGest/AutoGest
dotnet run --project AutoGest/AutoGest --no-launch-profile --urls http://localhost:5080 --environment Development
```

5. Abrí **http://localhost:5080**. En Visual Studio, seleccioná `AutoGest` como proyecto de inicio y ejecutalo con el perfil habitual.

`appsettings.json` incluye una conexión de ejemplo sin contraseña. La configuración de User Secrets la reemplaza en Development. También podés usar la variable `ConnectionStrings__GestoriaDb` en cualquier ambiente.

### Tu base existente

La migración nueva `ClientesComprasVentas` **crea Clientes, Compras y Ventas**, sus índices únicos y las claves foráneas. Las dos migraciones originales se conservan y la nueva no borra ni modifica vehículos. Los vehículos que ya tenés se consideran disponibles hasta que registres su venta.

El archivo `.rar` no incluía un respaldo de la base ni los User Secrets de tu computadora; por eso debés configurar tu conexión local antes de ejecutarlo. No uses `EnsureCreated` para una base con migraciones. Si tu esquema original fue creado manualmente y no tiene `__EFMigrationsHistory`, revisá su correspondencia con las migraciones antes de actualizarlas.

### Datos de demostración opcionales

Las marcas originales se cargan desde las migraciones. Para agregar además un contacto ficticio y un Corolla de ejemplo:

```sh
dotnet run --project AutoGest/AutoGest --no-launch-profile --urls http://localhost:5080 --environment Development --Database:SeedDemo=true
```

Este seed usa un documento y una patente de demostración para no repetir sus registros al reiniciar. No se activa por defecto ni genera operaciones.

## Alternativa con Docker

Copiá `.env.example` a `.env`, elegí una contraseña local y ejecutá:

```sh
docker compose up --build
```

Abrí **http://localhost:5080**. La base se guarda en un volumen y las migraciones se aplican al iniciar. `docker compose down` detiene los servicios y conserva los datos. La configuración incluida es para desarrollo local.

## Reglas del stock y de las operaciones

1. Cada vehículo del catálogo representa **una unidad física**, incluso si dos unidades tienen igual marca y modelo. Las patentes no vacías deben ser únicas.
2. Un vehículo registrado empieza **disponible**. Una compra registra su adquisición y costo; no crea una segunda unidad. Para comprar otra unidad, primero registrá su vehículo.
3. Cada unidad admite **una compra y una venta**. No se admite recompra/reventa de la misma ficha; un nuevo ciclo requiere otra ficha de vehículo. Las unidades iniciales pueden venderse sin compra cargada y muestran “Sin registrar” como costo.
4. Guardar una venta cambia el stock a **vendido**. Eliminarla libera la unidad. Editar la operación permite corregir contacto, fecha, importe y notas; el vehículo queda fijo.
5. La venta no puede ser anterior a la compra ni las operaciones tener fecha futura. Una compra de una unidad ya vendida no puede crearse. Una compra ya registrada puede corregirse respetando la fecha de venta.
6. No se elimina una compra con venta asociada. Si se elimina una compra sin venta, la unidad permanece disponible en el inventario y pierde su costo registrado.
7. Las claves foráneas impiden eliminar clientes o vehículos con operaciones relacionadas. Los índices únicos y bloqueos transaccionales en MySQL impiden ventas/compras duplicadas, también entre circuitos simultáneos.
8. El stock no tiene cantidades editables: se modifica registrando vehículos, compras y ventas. Los indicadores y tablas se actualizan mediante un servicio de estado con eventos.

## Estructura

```text
AutoGest/
  AutoGest/
    Components/Layout/    # Layout y menú adaptable
    Components/Pages/     # Páginas con rutas parametrizadas
    Components/Shared/    # Formularios, tablas, búsqueda y paginación reutilizables
    Data/                 # DbContext y seed opcional
    Models/               # Entidades y validaciones
    Services/             # Reglas, consultas, estado reactivo y DTO
    Migrations/           # Migraciones originales y ampliación
    wwwroot/              # Bootstrap local, CSS y JavaScript
  AutoGest.Tests/         # Validaciones e integración con MySQL real
.github/workflows/        # Build, tests, publish y deploy
```

## Verificación

```sh
dotnet build AutoGest/AutoGest.sln -c Release --warnaserror
dotnet test AutoGest/AutoGest.sln -c Release
dotnet ef migrations has-pending-model-changes --project AutoGest/AutoGest
```

Las pruebas de integración se omiten si no se define `AUTOGEST_TEST_CONNECTION_STRING`. Para ejecutarlas, prepará una base **descartable cuyo nombre termine en `_test`**, con un usuario que pueda crear tablas. Las pruebas migran la base y limpian sus clientes, vehículos y operaciones en cada caso; no uses la base de trabajo.

PowerShell:

```powershell
$env:AUTOGEST_TEST_CONNECTION_STRING = "Server=localhost;Database=autogest_test;User=autogest;Password=TU_CLAVE_LOCAL"
dotnet test AutoGest/AutoGest.sln -c Release
```

Bash:

```sh
AUTOGEST_TEST_CONNECTION_STRING='Server=localhost;Database=autogest_test;User=autogest;Password=TU_CLAVE_LOCAL' dotnet test AutoGest/AutoGest.sln -c Release
```

Se comprueban CRUD, búsquedas, paginación, documentos duplicados, validaciones, conflictos de edición, fechas coherentes, claves foráneas, dos ventas simultáneas y recuperación del stock al eliminar una venta. CI ejecuta esas pruebas con MySQL 8.4.

## GitHub y despliegue automático

El código y un historial local con Conventional Commits se entregan preparados para subir a tu repositorio. El ZIP incluye la carpeta oculta `.git` con ese historial; no contiene credenciales ni archivos de compilación. El workflow `.github/workflows/ci-cd.yml`:

1. En cada PR o push a `main`, restaura y compila con warnings como errores, revisa que no falten migraciones, ejecuta tests con MySQL real y publica un artefacto.
2. En cada push a `main`, despliega ese artefacto en **Azure App Service**, una vez configurada la variable `AZURE_WEBAPP_NAME` y el secreto `AZURE_WEBAPP_PUBLISH_PROFILE` en GitHub. Si falta la variable, el job de deploy se omite y CI sigue funcionando.

Configuración del hosting:

- Creá un App Service Linux con runtime **.NET 8** y una instancia MySQL accesible desde ese servicio.
- Activá **WebSockets**, necesarios para la interacción Blazor Server, y HTTPS. En Azure Linux, configurá `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para reconocer HTTPS detrás del proxy.
- Definí `ConnectionStrings__GestoriaDb` con la conexión de producción; usá TLS según los requisitos de tu servidor (por ejemplo `SslMode=Required`).
- Aplicá las migraciones con `dotnet ef database update` usando esa conexión o habilitá `Database__ApplyMigrations=true` para un despliegue de una sola instancia. No habilites el seed de demostración en la base de trabajo.
- Configurá `TZ=America/Argentina/Buenos_Aires` si querés que fechas e indicadores mensuales sigan la hora argentina.
- En GitHub, agregá el nombre de la aplicación como variable `AZURE_WEBAPP_NAME` y su perfil de publicación como secreto `AZURE_WEBAPP_PUBLISH_PROFILE`. El endpoint `/health` permite comprobar que el proceso arrancó.

**Estado de publicación:** el archivo entregado no está publicado en GitHub ni tiene URL pública. Hace falta conectar tu repositorio y un hosting para completar esos dos puntos de la consigna. El workflow y el Dockerfile están incluidos. La aplicación conserva el alcance académico original, sin autenticación ni roles de usuario.

## Correspondencia con la consigna

| Unidad | Implementación |
| --- | --- |
| 1 · Componentes | Más de 6 componentes reutilizables, `EditForm`, Data Annotations e `IValidatableObject`, rutas con `Id`, navegación programática, `GestionState` y eventos |
| 2 · Interfaz | Landmarks HTML5, Bootstrap 5 mobile-first local, CSS Isolation en layout, menú, cabeceras, métricas y buscador; JS Interop para CSV y confirmación |
| 3 · Persistencia | MySQL autorizado por el docente, EF Core/Pomelo, `IDbContextFactory`, entidades relacionadas, migraciones, seed, CRUD, búsqueda y paginación |
| 4 · Prácticas | README, historial local con Conventional Commits, workflow CI/CD y Dockerfile; publicación remota y URL pública pendientes de configurar |
