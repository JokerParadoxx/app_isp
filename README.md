# ISP PLC

Aplicación de escritorio en C# (.NET 8, Windows Forms) para operar un ISP.
El archivo que abre el programa es `Program.cs`, en la raíz de `app_isp`.

## Carpetas

- `modelo`: tablas de la base, sesión y conexión a MySQL.
- `controlador`: reglas de negocio, permisos y CRUD.
- `vista`: login, menú y pantallas.

La plantilla vacía `WinFormsApp1` no forma parte de esta aplicación.

## Antes de ejecutar

1. Tener instalado el SDK de .NET 8 (o uno más nuevo que pueda compilar `net8.0-windows`).
2. Tener MySQL en marcha.
3. Ejecutar el script `ddl base de datos.txt` para crear la base `isp_db` y los roles.
4. Revisar `appsettings.json` (host, puerto, base, usuario y contraseña).

## Compilar y abrir

Desde la carpeta `app_isp`:

```text
dotnet build
dotnet run
```

También se puede abrir `AppIsp.slnx` en Visual Studio y pulsar iniciar.
Si el equipo no tiene el runtime de escritorio de .NET 8, el proyecto usa el de .NET 10 (ya está configurado).

Para comprobar solo la conexión, sin ventanas:

```text
dotnet run -- --probar-conexion
```

## Primer ingreso

La cuenta que ya está en la base (`hehumam@gmail.com`) entra como maestro, con la contraseña guardada en `password_hash`. Desde **Usuarios y roles** puede crear y modificar cuentas y asignarles Comercial, Técnico o Maestro.

## Permisos

| Módulo | Comercial | Técnico | Maestro |
| --- | --- | --- | --- |
| Clientes | Crear y editar | No | Todo, incluido eliminar |
| Servicios | Alta, baja y cambio de plan | Solo lectura | Todo |
| Instalaciones | Crear y editar | Solo el estado | Todo |
| Tickets | Solo comerciales | Solo técnicos | Ambos |
| Agenda | Solo lectura | Solo lectura | Cargar bloques |
| Routers | No | Leer y actualizar | Todo |
| Configuración remota | No | Sí | Sí |
| Estado de la conexión | No | Sí | Sí |
| Aprovisionamiento | No | Sí | Sí |
| Facturas y planes | No | No | Todo |
| Usuarios y auditoría | No | No | Todo |

En el inicio, el técnico y el maestro ven los accesos **Configuración remota** y **Estado de la conexión**. El comercial no los ve.

**Configuración remota** guarda, por cada router, el nombre, la contraseña y el cifrado de las redes Wi-Fi de 2.4 GHz y 5 GHz, el reenvío de puertos, la DMZ, el firewall y el resto de la seguridad, el filtrado MAC, los dispositivos conectados, las IP reservadas, IPv4, IPv6 y DNS.

**Estado de la conexión** mide la IP de gestión del router (latencia y pérdida) y muestra el historial de mediciones y cuántos dispositivos figuran conectados.

Esas tablas también están en `ddl base de datos.txt`. Si la base ya existía, la aplicación las crea la primera vez que un técnico o el maestro abre uno de esos módulos.

## Agendamiento

En Clientes, Usuarios, Agenda, Instalaciones y Aprovisionamiento, crear o modificar abre una ventana con la ficha. La lista se queda en la pantalla de atrás. Un doble clic en la fila equivale a Modificar.

En Aprovisionamiento el identificador del equipo es la MAC. La ventana muestra esa MAC, el id y el nombre del cliente asociado, y el perfil del plan (nombre, descripción, velocidad y precio). El vínculo sale del servicio asignado al router.

En **Instalaciones** (botón Nueva) y en **Agenda** (botón Nuevo) el día se elige en un calendario visible. Los días anteriores a hoy aparecen bloqueados. Hoy y los días siguientes sí se pueden marcar. En una visita también se indica la hora.

En **Agenda**, el bloque horario es de una hora, desde las 09:00 hasta las 20:00 (el último rango es 19:00-20:00). Si la base todavía tiene mañana, tarde y noche, la aplicación amplía esa columna la primera vez que se abre la agenda.

## Manual

El uso de cada pantalla está en `manual de usuario.txt`, en esta misma carpeta.
