# E2 — Catálogos, centros, servicios y profesionales

## Qué incluye
**Catálogos por sociedad** (especialidades, categorías profesionales, competencias): alta, edición y desactivación (nunca se borran, para no romper referencias). Una especialidad nueva es un dato, no código.

**Centros** (hospital, clínica, centro ambulatorio, otro) con zona horaria IANA propia, activación/desactivación y responsables de centro.

**Servicios asistenciales** de un centro con:
- categorías profesionales admitidas (vacío = cualquiera) y competencias requeridas;
- coordinador del servicio (usuario con rol de coordinador médico);
- **tipos de turno propios** (hora local de inicio/fin; si el fin es igual o anterior al inicio, termina al día siguiente; inicio = fin equivale a 24 h) — «no todos los servicios usan los mismos turnos»;
- **requisitos de cobertura**: nº de profesionales por tipo de turno y, opcionalmente, categoría (base de la «necesidad de cobertura» de E3);
- configuración de reglas específicas en JSON validado (la interpretará el motor de reglas, E4).

**Profesionales**: datos de contacto mínimos (nombre, apellidos, correo, teléfono, nº de colegiado; sin DNI), categoría, especialidades, competencias con **fecha de caducidad opcional**, servicios autorizados, usuario vinculado opcional, activación/desactivación e **historial de cambios** (altas/bajas de especialidades, competencias y servicios y modificaciones de la ficha, con autor y fecha).

**Elegibilidad** (`GET /api/servicios/{id}/profesionales-elegibles?fecha=&incluirNoElegibles=`): regla de dominio que decide si un profesional *puede* ser asignado a un servicio en una fecha: activo, servicio activo, autorizado, categoría admitida y competencias requeridas vigentes (la fecha de caducidad es inclusiva). Devuelve el motivo de cada exclusión. Solapamientos, ausencias y descansos los valida el motor de reglas (E4). Será la base de la búsqueda de candidatos de E6.

## Permisos
| | Admin. sociedad | Coordinador | Resp. centro | Profesional |
|---|---|---|---|---|
| Catálogos: leer | ✔ | ✔ | ✔ | ✔ |
| Catálogos, centros, servicios, profesionales: escribir | ✔ | — | — | — |
| Centros, servicios, profesionales: leer | todo | todo | solo sus centros¹ | — |
| Historial de un profesional | ✔ | — | — | — |
| Candidatos elegibles | ✔ | ✔ | — | — |
| Su propia ficha (`/profesionales/mi-perfil`) | — | — | — | ✔ |

¹ El responsable de centro ve solo sus centros, los servicios de esos centros y los profesionales autorizados en ellos, **sin datos de contacto**.

Todos los permisos se aplican en el backend (políticas con nombre que además exigen sesión completa, sin MFA pendiente). El frontend solo oculta opciones.

## Aislamiento entre sociedades
- Cada entidad hereda de `EntidadDeOrganizacion` y el filtro global se aplica **automáticamente** a todas las que implementan `IPerteneceAOrganizacion`; una prueba falla si alguna quedara sin filtro.
- Referencias cruzadas (categoría, competencia, servicio, centro, usuario…) se resuelven con consultas filtradas: un id de otra sociedad es «inexistente» → 400 (en un cuerpo) o 404 (en la ruta).
- Los nombres son únicos **dentro** de cada sociedad (el mismo nombre puede existir en otra).

## Datos de demostración
Herramienta `backend/tools/MedicalHub.DatosDemo` (identificable: sociedad «[DEMO] …», usuarios `@demo.medicalhub.local`):

```bash
cd backend
export ConnectionStrings__Conn='<cadena>'
export DEMO_CONTRASENA='<≥ 12 caracteres, la eliges tú>'
dotnet run --project tools/MedicalHub.DatosDemo -- --confirmar            # crea la sociedad demo
dotnet run --project tools/MedicalHub.DatosDemo -- --confirmar --eliminar # la elimina por completo
```
Crea 3 centros (uno en `Atlantic/Canary`), 4 servicios con turnos de mañana, tarde, noche (22:00–08:00) y guardia de 24 h, requisitos de cobertura, catálogos, 12 profesionales (uno con una competencia caducada para probar la elegibilidad), un coordinador, un responsable de centro y profesionales con usuario. No hay contraseña por defecto, es idempotente y se niega a ejecutarse con `ASPNETCORE_ENVIRONMENT=Production` sin `--permitir-produccion`. La sociedad demo tiene el MFA desactivado.

## Decisiones técnicas
- Las colecciones de pertenencia (especialidades, competencias, servicios autorizados, categorías/competencias de un servicio, responsables) son tipos poseídos con **clave `Guid` sin valor generado**; con la convención por defecto EF no insertaba los elementos añadidos a una entidad ya cargada. Hay una prueba de API que lo cubre.
- La auditoría registra las altas y bajas de esas colecciones bajo el id de su propietario (alimenta el historial).
- Los textos de «reglas específicas» se guardan como JSON validado, sin interpretar todavía.

## No incluido en E2 (según el plan)
Disponibilidad y calendario personal del profesional (E5), historial de **asignaciones** (E3), pantalla de candidatos elegibles en la interfaz (E6), carga masiva de profesionales.
