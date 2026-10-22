# E1 — Identidad y multiempresa

## Qué incluye
- **Organizaciones** (sociedades) y **usuarios** con los cinco roles: `AdministradorPlataforma`, `AdministradorSociedad`, `CoordinadorMedico`, `ProfesionalSanitario`, `ResponsableCentro`.
- **Autenticación**: JWT de acceso (15 min, en memoria en el cliente) + token de refresco opaco y rotatorio (7 días, solo se guarda su hash SHA-256).
- **Protección de cuentas**: bloqueo tras 5 fallos (15 min), límite de peticiones de login/refresco por IP, mensaje idéntico exista o no el usuario, política de contraseña (≥ 12 caracteres), cambio de contraseña que revoca todas las sesiones.
- **MFA TOTP** (RFC 6238, compatible con Google/Microsoft Authenticator). Obligatorio para el administrador de plataforma siempre, y para administradores y coordinadores de una sociedad si su política `MfaObligatorio` está activa (por defecto, sí). Hasta que se activa, la sesión queda «MFA pendiente» y solo puede usar los endpoints de MFA, perfil, cambio de contraseña y cierre de sesión. Los secretos se cifran con Data Protection y cada código solo vale una vez.
- **Auditoría** de cambios en organizaciones, usuarios y roles (sin contraseñas ni secretos) y **eventos de seguridad** (accesos correctos/fallidos, bloqueos, MFA, reutilización de refresco).
- **Frontend**: login con segundo factor, alta de MFA con QR, restauración de sesión, renovación automática del token, navegación adaptada por rol y tamaño, gestión de usuarios (sociedad) y de sociedades (plataforma).

## Cómo se garantiza el aislamiento entre sociedades
1. La organización **sale únicamente del token validado** (`org`); ningún contrato de entrada acepta un `organizacionId`.
2. `MedicalHubDbContext` aplica un **filtro global de consulta** a toda entidad `IPerteneceAOrganizacion`. Una prueba automática falla si se añade una entidad sin filtro.
3. El contexto **rechaza escrituras** de filas de otra organización (defensa en profundidad).
4. Consultas sin sesión o de plataforma solo ven filas sin organización. `IgnoreQueryFilters()` se usa solo en el login/refresco (donde aún no hay organización) y para comprobar unicidad de correo.
5. Un identificador de otra sociedad responde **404** (no 403), sin revelar su existencia.
6. El administrador de plataforma **no** accede a los datos operativos de las sociedades (política y filtro lo impiden).

## Primer administrador de plataforma (sin credenciales por defecto)
Al arrancar, si no existe ningún administrador de plataforma y hay configuración, se crea uno:

```bash
export Bootstrap__AdministradorPlataforma__Email=<correo>
export Bootstrap__AdministradorPlataforma__Contrasena=<contraseña de ≥ 12 caracteres>
```
En producción, defínalas una vez y **retírelas** después. En desarrollo pueden ir en `dotnet user-secrets`.

## Configuración de seguridad (`Seguridad__*`)
| Clave | Por defecto | Nota |
|---|---|---|
| `ClaveJwt` | — | **Obligatoria fuera de Desarrollo** (≥ 32 caracteres, variable de entorno). En Desarrollo se genera una efímera. |
| `MaxIntentosFallidos` / `MinutosBloqueo` | 5 / 15 | |
| `MinutosTokenAcceso` / `DiasTokenRefresco` | 15 / 7 | |
| `LimiteLoginPorMinuto` | 10 | por IP |
| `DataProtection__RutaClaves` | — | Ruta persistente del anillo de claves (necesaria en producción; si se pierde, hay que reiniciar el MFA de los usuarios). |
| `Base__MigrarAlIniciar` | false (true en Desarrollo) | |

## Decisiones y desviaciones respecto a la Fase 1
- **Token de refresco en el cuerpo de la respuesta**, no en cookie `HttpOnly`, para usar el mismo flujo en web, móvil y escritorio. En web se guarda con `flutter_secure_storage` (almacenamiento del navegador): es más expuesto a XSS que una cookie. Mitigaciones: rotación con detección de reutilización, expiración corta del acceso y revocación al cambiar contraseña. Si se prefiere cookie en web, es un cambio acotado (E8, junto a la revisión de seguridad).
- **Application referencia EF Core** (interfaz `IAppDbContext`) para evitar una capa de repositorios sin valor añadido.
- **Validación propia** (clase `Validador`), sin FluentValidation, como decisión por defecto de la Fase 1.
- Roles asignables en una sociedad: todos salvo `AdministradorPlataforma`. El alcance de `ResponsableCentro` por centro llega con los centros (E2).

## Pendiente / no incluido en E1
- Recuperación de contraseña por correo (requiere el canal de email, E8).
- Registro de eventos `AccesoDenegado` (403) en los eventos de seguridad.
- Pantalla para que cada usuario cambie su contraseña (el endpoint existe).
- Ver eventos de seguridad y auditoría desde la interfaz (llegará con los informes).
