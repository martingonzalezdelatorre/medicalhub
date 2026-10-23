# Documentación RGPD / LOPDGDD (borrador técnico)

> **Aviso.** Este documento describe qué datos trata el sistema y con qué medidas técnicas, para que la sociedad usuaria pueda elaborar su propia documentación. **No es asesoramiento jurídico**: debe revisarlo el delegado de protección de datos (DPO) o un asesor legal antes de usarlo como registro oficial. Los plazos y bases jurídicas son propuestas.

## 1. Roles
- **Responsable del tratamiento:** cada sociedad médica cliente (decide fines y medios respecto de sus profesionales).
- **Encargado del tratamiento:** el proveedor que aloja y opera MedicalHub. Se necesita un contrato de encargo (art. 28 RGPD) que regule instrucciones, confidencialidad, subencargados, medidas, asistencia en derechos y devolución o supresión de los datos.
- **Subencargados previsibles:** proveedor de alojamiento/nube y de copias de seguridad (a identificar por el proveedor).

## 2. Principio de minimización: qué NO se trata
- **No hay datos de pacientes ni historia clínica.**
- **No se registran diagnósticos ni motivos médicos.** Las ausencias solo guardan el *tipo* (vacaciones, asuntos propios, permiso, formación, baja temporal, otro), las fechas y un comentario opcional de 300 caracteres. La interfaz avisa de no escribir datos de salud, y el texto de las notificaciones nunca incluye ese comentario.
- No se guardan DNI ni datos bancarios.

## 3. Registro de actividades de tratamiento (propuesta)
**Tratamiento: gestión de turnos, disponibilidad y cobertura de profesionales sanitarios.**

| Aspecto | Contenido |
|---|---|
| Finalidad | Planificar turnos y guardias, gestionar ausencias y sustituciones, controlar el cumplimiento horario, informar a los interesados y obtener informes de gestión. |
| Interesados | Profesionales sanitarios de la sociedad; usuarios de gestión (administradores, coordinadores, responsables de centro). |
| Categorías de datos | **Identificación y contacto:** nombre, apellidos, correo, teléfono, número de colegiado (opcionales salvo nombre). **Profesionales:** categoría, competencias y su vigencia, servicios autorizados. **Laborales:** turnos asignados, horas planificadas y trabajadas, fichajes (entrada, pausas, salida y, en entrada y salida, coordenadas del dispositivo con su precisión y si cayeron fuera de la geovalla del centro), ajustes con motivo, disponibilidad y preferencias, ausencias (tipo y fechas), ofertas de sustitución. **Cuenta:** correo, hash de contraseña, roles, estado de MFA (secreto cifrado). **Trazas:** registros de auditoría y eventos de seguridad (incluye IP y correo intentado). |
| Categorías especiales (art. 9) | No se tratan de forma intencionada. Riesgo residual: texto libre de comentarios (ver §6). |
| Base jurídica (propuesta) | Ejecución del contrato de trabajo o relación estatutaria y cumplimiento de obligaciones legales (control horario) —art. 6.1.b y 6.1.c—; interés legítimo de la organización para la seguridad del sistema —art. 6.1.f—. El DPO debe confirmarlo. |
| Destinatarios | Personal autorizado de la propia sociedad según su rol. El proveedor como encargado. No hay cesiones a terceros ni transferencias internacionales por defecto (depende del hosting elegido). |
| Plazos de conservación | Ver §5. |
| Medidas de seguridad | Ver §4 y `docs/seguridad.md`. |

## 4. Medidas técnicas y organizativas implantadas
- **Aislamiento entre sociedades:** toda tabla de negocio lleva la organización y el sistema aplica un filtro automático; la organización sale solo del token validado, nunca del cliente. Hay pruebas automáticas de aislamiento en cada módulo.
- **Control de acceso por roles** aplicado en el servidor (administrador de plataforma, administrador de sociedad, coordinador médico, responsable de centro con alcance por centros, profesional con acceso solo a lo suyo). Una prueba automática impide que un endpoint quede sin autorización explícita.
- **Autenticación:** contraseñas con hash adaptativo y longitud mínima de 12; bloqueo por intentos fallidos; sesión con token de acceso de 15 minutos y token de refresco rotatorio con detección de reutilización; **MFA TOTP obligatorio** para roles de gestión; límite de intentos por IP.
- **Cifrado:** en tránsito (HTTPS y HSTS); secretos MFA cifrados con Data Protection; el cifrado en reposo de la base y de las copias depende de la infraestructura (activar TDE o cifrado de disco/almacenamiento).
- **Auditoría:** cambios en entidades de negocio y eventos de seguridad (inicios de sesión, bloqueos, exportaciones). Excepciones a reglas de planificación con justificación y autor, inmutables.
- **Exportaciones:** solo administración y coordinación; cada descarga queda registrada; se neutralizan fórmulas en CSV.
- **Copias de seguridad:** verificadas y restaurables (`docs/e8-instalacion-y-operacion.md`).
- **Secretos:** fuera del repositorio; usuario de base de datos de mínimo privilegio.

## 5. Retención y borrado
Depuración automática diaria (`Retencion__*`, 0 = desactivada). Valores por defecto propuestos:

| Dato | Plazo por defecto |
|---|---|
| Notificaciones leídas | 90 días |
| Notificaciones (cualquiera) | 365 días |
| Eventos de seguridad | 365 días |
| Tokens de refresco caducados | 30 días tras caducar |
| Registros de auditoría | 5 años |
| Coordenadas de fichajes | 90 días (después se anonimizan; el fichaje y sus horas se conservan) |

**Pendiente de definir por cada sociedad:** plazo de conservación de turnos, horas y ausencias (p. ej. el que exija la normativa laboral de control horario). El sistema **no los borra automáticamente**; las bajas de profesionales se hacen por desactivación y la supresión definitiva debe tramitarse como solicitud al proveedor (ver §7).

## 6. Evaluación de impacto (orientativa, art. 35)
| Riesgo | Probabilidad / impacto | Medida |
|---|---|---|
| Acceso indebido a datos de otra sociedad | Baja / Alto | Filtro automático por organización, pruebas de aislamiento, org solo desde el token. |
| Suplantación de usuario de gestión | Media / Alto | MFA obligatorio, bloqueo, rotación de tokens, límite de intentos. |
| Datos de salud en comentarios libres | Media / Alto | Aviso en la interfaz, comentario corto, sin mostrarlo en notificaciones; formación de usuarios; el DPO valora si desactivar el campo. |
| Fuga por exportación o copia de seguridad | Media / Medio | Exportaciones restringidas y auditadas; copias cifradas y con acceso restringido. |
| Pérdida de datos | Baja / Alto | Copias verificadas y ensayos de restauración. |
| Exceso de conservación | Media / Medio | Depuración automática; plazos a fijar por la sociedad. |
| Geolocalización en el fichaje | Media / Medio | Solo se lee la ubicación al fichar entrada y salida (nunca seguimiento continuo); se informa al profesional en la app; las coordenadas se anonimizan a los 90 días y no se copian a la auditoría; no bloquea el fichaje, solo lo marca para revisión; acceso limitado a coordinación. |

Conclusión orientativa: el tratamiento no incluye datos clínicos ni seguimiento continuo de personas (la ubicación se toma solo en cada fichaje, y su uso para el control horario debe comunicarse a los profesionales y, en su caso, a la representación legal de los trabajadores); el DPO debe decidir si procede una EIPD formal (el control horario y el tamaño del colectivo pueden influir).

## 7. Derechos de los interesados
| Derecho | Cómo se atiende hoy |
|---|---|
| Acceso | El profesional ve en la aplicación sus turnos, ausencias, disponibilidad, ofertas y horas. Para el resto (p. ej. trazas), la sociedad solicita un extracto al proveedor. |
| Rectificación | La coordinación edita la ficha del profesional; el profesional puede pedirlo a su coordinación. |
| Supresión / limitación | Desactivación del profesional y de su usuario (conserva lo necesario por obligación legal). La supresión definitiva la ejecuta el proveedor a petición del responsable. |
| Portabilidad | Informes en CSV. |
| Oposición | A través del responsable (la sociedad). |

**Hueco conocido:** no hay todavía una función «borrar a este profesional» ni «exportar todos mis datos» automáticas; se hacen por procedimiento manual del proveedor y conviene automatizarlas si el volumen lo justifica.

## 8. Incidentes (brechas)
Notificación a la AEPD en 72 horas y, si procede, a los afectados. Fuentes de detección: eventos de seguridad (reutilización de tokens, bloqueos, accesos denegados), registros de la API y alertas del hosting. El proveedor debe avisar al responsable «sin dilación indebida» (contrato de encargo).

## 9. Lista de comprobación antes de ir a producción
- [ ] Contrato de encargo del tratamiento firmado.
- [ ] Registro de actividades y plazos de conservación aprobados por el DPO.
- [ ] Información a los profesionales (art. 13) y política sobre comentarios sin datos de salud.
- [ ] Cifrado en reposo activado; copias cifradas; ensayo de restauración hecho.
- [ ] MFA activado para todos los usuarios de gestión; administrador de plataforma de uso restringido y auditado.
- [ ] Variables `Bootstrap__*` retiradas; contraseña de `sa` rotada y sin uso por la aplicación.
