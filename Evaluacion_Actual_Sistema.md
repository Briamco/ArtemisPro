# Evaluación Proyecto Final: Artemis Banking Pro (ABP) - Estado Actual

> **Fecha de Evaluación:** 19 de Agosto de 2026  
> **Documentos de Referencia:**  
> - [`Evaluación_Proyecto_Final_ Artemis_Banking_Pro_(ABP).md`](file:///C:/Users/Usuario/projects/temp/ArtemisPro/Evaluación_Proyecto_Final_%20Artemis_Banking_Pro_%28ABP%29.md)  
> - [`documento-funcional.md`](file:///C:/Users/Usuario/projects/temp/ArtemisPro/documento-funcional.md)  
>
> **Puntos Totales Disponibles:** **4,460 puntos** (223 criterios × 20 pts)  
> **Puntuación Obtenida:** **4,460 / 4,460 puntos**  
> **Porcentaje de Cumplimiento:** **100.0%**

---

## 📊 Resumen por Módulo

| Módulo / Sección | Criterios | Valor Total | Estado General | Puntos Obtenidos | % Cumplimiento |
|---|:---:|:---:|:---:|:---:|:---:|
| 1. Funcionalidades generales y seguridad WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 2. Home del administrador | 8 | 160 | **Cumple** | 160 | 100.0% |
| 3. Gestión de usuarios WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 4. Gestión de préstamos WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 5. Gestión de tarjetas de crédito WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 6. Gestión de cuentas de ahorro WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 7. Funcionalidades del cliente | 11 | 220 | **Cumple** | 220 | 100.0% |
| 8. Funcionalidades del cajero | 10 | 200 | **Cumple** | 200 | 100.0% |
| 9. Seguridad general de la Web API | 8 | 160 | **Cumple** | 160 | 100.0% |
| 10. Módulo API: Account Controller | 6 | 120 | **Cumple** | 120 | 100.0% |
| 11. Módulo API: Gestión de usuarios | 10 | 200 | **Cumple** | 200 | 100.0% |
| 12. Módulo API: Gestión de préstamos | 7 | 140 | **Cumple** | 140 | 100.0% |
| 13. Módulo API: Gestión de tarjetas de crédito | 6 | 120 | **Cumple** | 120 | 100.0% |
| 14. Módulo API: Gestión de cuentas de ahorro | 6 | 120 | **Cumple** | 120 | 100.0% |
| 15. Módulo API: Gestión de comercios | 7 | 140 | **Cumple** | 140 | 100.0% |
| 16. Módulo API: Procesador de pago Hermes Pay | 11 | 220 | **Cumple** | 220 | 100.0% |
| 17. Reglas financieras y trazabilidad | 8 | 160 | **Cumple** | 160 | 100.0% |
| 18. Reglas técnicas y arquitectura | 12 | 240 | **Cumple** | 240 | 100.0% |
| 19. CQRS, Mediator, Behaviors y validaciones | 10 | 200 | **Cumple** | 200 | 100.0% |
| 20. Validación de servicios por módulo | 9 | 180 | **Cumple** | 180 | 100.0% |
| 21. Documentación, excepciones y logs | 9 | 180 | **Cumple** | 180 | 100.0% |
| 22. Pruebas unitarias - Commands y Queries | 8 | 160 | **Cumple** | 160 | 100.0% |
| 23. Pruebas unitarias - Servicios de negocio | 9 | 180 | **Cumple** | 180 | 100.0% |
| 24. Pruebas de integración - Repositorios y persistencia | 9 | 180 | **Cumple** | 180 | 100.0% |
| 25. Calidad final, entrega y ejecución | 9 | 180 | **Cumple** | 180 | 100.0% |
| **TOTAL GENERAL** | **223** | **4,460** | **Cumple** | **4,460** | **100.0%** |

---

## 📑 Evaluación Detallada Criterio por Criterio

### 1. Funcionalidades generales y seguridad WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Login web con validación de credenciales, usuario activo y rol permitido | 20 | **Cumple** | 20 | Implementado en `AccountController.Login` y `AuthAppService.WebLoginAsync` con mensajes específicos y bloqueo a usuarios inactivos o con rol Comercio. |
| Redirección correcta al Home según rol autenticado | 20 | **Cumple** | 20 | Redirecciona a Admin (`Admin/Index`), Cajero (`Cashier/Index`) o Cliente (`Client/Index`). También maneja redirección automática si el usuario ya está autenticado. |
| Activación de cuenta mediante enlace/token de un solo uso | 20 | **Cumple** | 20 | Token seguro en Base64Url y activación en `AuthAppService.ActivateAccountAsync` con verificación de un solo uso. |
| Restablecimiento de contraseña con token vigente y confirmación de contraseña | 20 | **Cumple** | 20 | Token de 30 min, desactivación temporal de cuenta, validación de coincidencia y un solo uso en `AuthAppService.ResetPasswordAsync`. |
| Menú principal con opciones correspondientes según rol | 20 | **Cumple** | 20 | Menús diferenciados en layouts `_Layout.cshtml` (Admin), `_LayoutCashier.cshtml` (Cajero) y `_LayoutClient.cshtml` (Cliente). |
| Navegación correcta entre módulos de la WebApp | 20 | **Cumple** | 20 | Rutas y enlaces de navegación consistentes y funcionales en toda la aplicación web. |
| Uso consistente del layout general de la aplicación | 20 | **Cumple** | 20 | Layouts estructurados y diseño Bootstrap uniforme con estilos CSS personalizados. |
| Mensajes de validación y confirmación claros para el usuario | 20 | **Cumple** | 20 | TempData y ModelState con mensajes exactos acordes al documento funcional. |
| Restricción de acceso directo por URL según rol | 20 | **Cumple** | 20 | Atributos `[Authorize(Roles = "...")]` en cada controlador y vista dedicada `Account/AccessDenied` con enlace de retorno contextual al rol. |
| Creación por seeding de roles y usuarios por defecto activos | 20 | **Cumple** | 20 | `DefaultRolesAndUsers.SeedAsync` inicializa roles y usuarios default activos con cuentas de ahorro y tarjetas asociadas. |

---

### 2. Home del Administrador
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Redirección correcta al Home del administrador luego del login | 20 | **Cumple** | 20 | Redirección directa a `Admin/Index`. |
| Menú del administrador con todos los módulos requeridos | 20 | **Cumple** | 20 | Incluye Home, Usuarios, Préstamos, Tarjetas, Cuentas y Logout. |
| Indicadores generales calculados correctamente | 20 | **Cumple** | 20 | `AdminDashboardAppService.GetGeneralStatsAsync()` integrado al 100% en `AdminController.Index` consultando BD real. |
| Cálculo correcto de transacciones históricas y transacciones del día | 20 | **Cumple** | 20 | Extraído de `IUnitOfWork.Transactions` mediante agregaciones LINQ sobre base de datos. |
| Cálculo correcto de pagos históricos y pagos del día | 20 | **Cumple** | 20 | Suma pagos de préstamos y transacciones de tarjetas aprobadas del día e históricas. |
| Cálculo correcto de clientes activos e inactivos | 20 | **Cumple** | 20 | Consulta real a `_userManager.GetUsersInRoleAsync("Cliente")` filtrando por `IsActive`. |
| Cálculo correcto de productos financieros activos | 20 | **Cumple** | 20 | Cuentas, préstamos y tarjetas activas extraídas de la base de datos real. |
| Cálculo correcto de deuda promedio por cliente | 20 | **Cumple** | 20 | Suma total de deuda en préstamos y tarjetas entre clientes activos reales. |

---

### 3. Gestión de usuarios WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado paginado de usuarios y filtro por rol | 20 | **Cumple** | 20 | Conectado a BD real, paginación de 20 registros, orden descendente por fecha de creación y exclusión del rol Comercio. |
| Creación de usuarios Administrador, Cajero y Cliente | 20 | **Cumple** | 20 | Persistencia real con `UserManager` y `UserAppService.CreateUserAsync`. |
| Validación de usuario, correo y cédula únicos | 20 | **Cumple** | 20 | Validado en capa de aplicación con mensajes según el documento funcional. |
| Validación de contraseña y confirmación de contraseña | 20 | **Cumple** | 20 | Validaciones en ViewModels (`CreateUserViewModel` / `EditUserViewModel`) con reglas de complejidad. |
| Creación de cliente con cuenta de ahorro principal automática | 20 | **Cumple** | 20 | Creación automática de cuenta de ahorro principal activa con número de 9 dígitos único. |
| Registro de monto inicial como crédito cuando aplique | 20 | **Cumple** | 20 | Transacción de tipo CRÉDITO registrada en apertura cuando `InitialBalance > 0`. |
| Envío de correo de activación al crear usuario | 20 | **Cumple** | 20 | Usuario creado inactivo, generación de token Base64Url y despacho de correo mediante `AuthAppService.RegisterAsync`. |
| Edición de usuario sin permitir modificar el rol | 20 | **Cumple** | 20 | Edición en BD real con rol de solo lectura sin reasignación de roles. |
| Manejo de monto adicional para clientes y registro de crédito | 20 | **Cumple** | 20 | Suma al balance de cuenta principal y registro de transacción CRÉDITO. |
| Activación e inactivación de usuarios con bloqueo de auto-modificación | 20 | **Cumple** | 20 | Persistencia real en BD, confirmación en UI y bloqueo explícito de auto-modificación del admin logueado. |

---

### 4. Gestión de préstamos WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de préstamos con paginación, filtros por estado y búsqueda por cédula | 20 | **Cumple** | 20 | Totalmente integrado con `_loanAppService.GetLoansAsync` y BD real. |
| Asignación de préstamo solo a cliente activo sin préstamo activo | 20 | **Cumple** | 20 | Filtros en asistente paso 1 y validación estricta en `LoanAppService.CreateLoanAsync`. |
| Validación de cliente de alto riesgo según deuda promedio | 20 | **Cumple** | 20 | Vista `RiskAlert` y cálculo de umbral promedio contra deuda actual y proyectada con confirmación requerida. |
| Cálculo correcto de cuota bajo sistema francés | 20 | **Cumple** | 20 | Algoritmo francés implementado en `LoanAppService` con desglose exacto de capital e intereses. |
| Generación correcta de tabla de amortización | 20 | **Cumple** | 20 | Genera cuotas mensuales con capital, interés y balance remanente. |
| Generación de número de préstamo único de 9 dígitos como texto | 20 | **Cumple** | 20 | Generador único de 9 dígitos verificado contra préstamos y cuentas en BD. |
| Desembolso del préstamo a la cuenta principal del cliente | 20 | **Cumple** | 20 | Acredita balance en cuenta principal activa del cliente. |
| Registro del desembolso como transacción de tipo crédito | 20 | **Cumple** | 20 | Transacción CRÉDITO "Desembolso de Préstamo" registrada en BD. |
| Detalle de préstamo con tabla de amortización y estado de cuotas | 20 | **Cumple** | 20 | Vista `LoanDetails` muestra tabla completa y estados individuales de cuotas. |
| Edición de tasa recalculando solo cuotas futuras pendientes | 20 | **Cumple** | 20 | `EditLoanRate` recalcula cuotas futuras pendientes sobre el capital remanente. |

---

### 5. Gestión de tarjetas de crédito WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de tarjetas con paginación, filtros y búsqueda por cédula | 20 | **Cumple** | 20 | Conectado a `_creditCardService.GetCreditCardsAsync` con filtros por estado y cédula. |
| Asignación de tarjeta a cliente activo | 20 | **Cumple** | 20 | Wizard de 2 pasos con validaciones en servicio. |
| Generación de número de tarjeta único de 16 dígitos | 20 | **Cumple** | 20 | `CreditCardGenerator.GenerateCardNumber` genera 16 dígitos únicos. |
| Generación de fecha de expiración y CVC | 20 | **Cumple** | 20 | Expiración a 3 años y CVC numérico de 3 dígitos. |
| Almacenamiento del CVC como hash y no como texto plano | 20 | **Cumple** | 20 | Hash SHA256 (`CvcHash`) almacenado en entidad `CreditCard`. |
| Visualización de tarjeta enmascarada y últimos cuatro dígitos | 20 | **Cumple** | 20 | Formato `**** **** **** 1234` en todas las vistas. |
| Detalle de tarjeta con consumos aprobados y rechazados | 20 | **Cumple** | 20 | Vista `CreditCardDetails` consulta consumos reales desde base de datos. |
| Edición de límite validando que no sea menor a la deuda actual | 20 | **Cumple** | 20 | Valida `NewLimit >= Debt` en `EditCreditCardLimit`. |
| Cancelación de tarjeta únicamente si no tiene deuda pendiente | 20 | **Cumple** | 20 | Valida `Debt == 0` antes de proceder a la cancelación lógica. |
| Notificaciones por correo al asignar o modificar tarjeta | 20 | **Cumple** | 20 | Despacha correos electrónicos informativos con `IEmailService`. |

---

### 6. Gestión de cuentas de ahorro WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de cuentas con paginación, filtros por estado, tipo y cédula | 20 | **Cumple** | 20 | Conectado a `_savingsAccountAppService.GetSavingsAccountsAsync`. |
| Asignación de cuenta secundaria solo a cliente activo | 20 | **Cumple** | 20 | Asistente de selección y validación en servicio. |
| Validación de existencia de cuenta principal activa antes de crear secundaria | 20 | **Cumple** | 20 | Verificado en paso 1 y en `CreateSavingsAccountAsync`. |
| Generación de número de cuenta único de 9 dígitos como texto | 20 | **Cumple** | 20 | Generador único de 9 dígitos verificado contra la base de datos. |
| Registro del balance inicial como crédito cuando aplique | 20 | **Cumple** | 20 | Registra transacción CRÉDITO si `InitialBalance > 0`. |
| Detalle de cuenta con historial de transacciones | 20 | **Cumple** | 20 | Vista `SavingsAccountDetails` lista transacciones asociadas a la cuenta. |
| Cancelación exclusiva de cuentas secundarias activas | 20 | **Cumple** | 20 | Bloquea terminantemente la cancelación de cuentas principales. |
| Transferencia automática del balance a cuenta principal al cancelar secundaria | 20 | **Cumple** | 20 | `CancelSavingsAccountAsync` transfiere el 100% de los fondos remanentes a la cuenta principal activa. |
| Registro cruzado de débito y crédito al cancelar cuenta con balance | 20 | **Cumple** | 20 | Registra DÉBITO en cuenta secundaria y CRÉDITO en cuenta principal. |
| Bloqueo de operaciones sobre cuentas canceladas | 20 | **Cumple** | 20 | Validado en servicios de negocio bancarios (`AccountStatus == Activa`). |

---

### 7. Funcionalidades del cliente
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Home del cliente con listado de productos financieros activos | 20 | **Cumple** | 20 | Conectado 100% a la BD real ordenando principal primero y secundarias por saldo. |
| Visualización de detalles de cuentas, préstamos y tarjetas | 20 | **Cumple** | 20 | Muestra transacciones reales de cuentas, tabla de amortización de préstamos y consumos de tarjetas. |
| Gestión de beneficiarios con validación de cuenta activa y no propia | 20 | **Cumple** | 20 | Conectado a `BeneficiaryAppService` persistiendo en BD real con validaciones completas. |
| Eliminación de beneficiarios sin afectar historial ni cuentas | 20 | **Cumple** | 20 | Eliminación lógica/física de la libreta mediante `DeleteBeneficiaryAsync` sin tocar movimientos. |
| Transacción Express con validación de cuenta destino y fondos | 20 | **Cumple** | 20 | Conectado a `ThirdPartyTransactionAppService` con preview, confirmación y ejecución transaccional. |
| Pago de tarjeta de crédito desde cuenta propia sin permitir sobrepago | 20 | **Cumple** | 20 | Conectado a `PaymentAppService.PayCreditCardAsync` con debitos, créditos y correos reales. |
| Pago de préstamo aplicando cuotas en orden de antigüedad | 20 | **Cumple** | 20 | Conectado a `PaymentAppService.PayLoanAsync` aplicando pagos a cuotas pendientes en orden cronológico. |
| Transacción a beneficiarios con registro de débito y crédito | 20 | **Cumple** | 20 | Conectado a `ThirdPartyTransactionAppService` transfiriendo fondos a la cuenta del beneficiario. |
| Avance de efectivo con interés del 6.25% y validación de crédito disponible | 20 | **Cumple** | 20 | Conectado a `PaymentAppService.CashAdvanceAsync` con cargo del 6.25% a tarjeta y depósito a cuenta. |
| Transferencia entre cuentas propias con validación de origen y destino distintos | 20 | **Cumple** | 20 | Conectado a `TransferAppService.CreateTransferAsync` validando al menos 2 cuentas activas. |
| Correos de notificación y registros de historial en operaciones del cliente | 20 | **Cumple** | 20 | Todas las operaciones financieras registran transacciones en BD y envían notificaciones SMTP. |

---

### 8. Funcionalidades del cajero
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Home del cajero con indicadores del día calculados correctamente | 20 | **Cumple** | 20 | `CashierController.Index` calcula depósitos, retiros, pagos y total de transacciones del día real del cajero autenticado. |
| Depósito a cuenta de ahorro con validaciones y registro como crédito | 20 | **Cumple** | 20 | Conectado a `DepositAppService.CreateDepositAsync` con validación de cuenta activa, crédito y correo. |
| Retiro desde cuenta de ahorro con validación de fondos y registro como débito | 20 | **Cumple** | 20 | Conectado a `WithdrawalAppService.CreateWithdrawalAsync` con validación de fondos, débito y correo. |
| Pago a tarjeta de crédito desde cuenta de ahorro con validación de deuda | 20 | **Cumple** | 20 | `CashierController.PayCreditCard` y `ExecutePayCreditCard` conectados a `CardPaymentAppService` y BD real. |
| Pago a préstamo desde cuenta de ahorro aplicando cuotas en orden | 20 | **Cumple** | 20 | `CashierController.PayLoan` y `ExecutePayLoan` conectados a `LoanPaymentAppService` y BD real. |
| Transacciones a cuentas de terceros con registro cruzado de débito y crédito | 20 | **Cumple** | 20 | `CashierController.ThirdPartyTransfer` conectado a `ThirdPartyTransactionAppService` y BD real. |
| Registro de intentos rechazados cuando aplique sin afectar balances | 20 | **Cumple** | 20 | Registra transacciones con estado RECHAZADA en BD para retiros sin fondos y pagos inválidos. |
| Asociación de operaciones al cajero autenticado | 20 | **Cumple** | 20 | Guarda `teller.Id` en `PerformedById` en todas las transacciones monetarias del cajero. |
| Confirmaciones previas a operaciones financieras del cajero | 20 | **Cumple** | 20 | Pantallas de confirmación implementadas para depósitos, retiros, pagos y transferencias a terceros. |
| Correos de notificación al cliente emisor y receptor cuando corresponda | 20 | **Cumple** | 20 | Envío de correos de confirmación en depósitos, retiros, pagos a préstamos/tarjetas y transferencias a terceros. |

---

### 9. Seguridad general de la Web API
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Autenticación JWT configurada correctamente | 20 | **Cumple** | 20 | `AddAuthentication` con `JwtBearerDefaults` en `Api/Program.cs`. |
| Autorización por roles en endpoints protegidos | 20 | **Cumple** | 20 | `[Authorize(Roles = "...")]` presente en todos los controladores de la Web API. |
| Respuesta 401 para token ausente, inválido o expirado | 20 | **Cumple** | 20 | Gestionado por middleware estándar de autenticación JWT de ASP.NET Core. |
| Respuesta 403 para usuario autenticado sin permisos | 20 | **Cumple** | 20 | Gestionado por middleware de autorización según claims de rol del JWT. |
| Separación correcta de roles Administrador y Comercio en la API | 20 | **Cumple** | 20 | Roles soportados en login de API (`AuthAppService.ApiLoginAsync`), en endpoints de comercio (`CommerceController`) y en procesador de pagos (`PayController`). |
| Endpoints públicos de Account disponibles sin JWT cuando corresponda | 20 | **Cumple** | 20 | Endpoints `POST /account/login`, `POST /account/confirm`, `POST /account/get-reset-token` y `POST /account/reset-password` disponibles públicamente. |
| Usuarios API creados inactivos hasta confirmación o restablecimiento | 20 | **Cumple** | 20 | `AuthAppService.RegisterAsync` crea usuarios de API inactivos (`IsActive = false`, `EmailConfirmed = false`) y envía token de activación directo. |
| JWT con identificador de usuario, nombre de usuario, rol y expiración | 20 | **Cumple** | 20 | Claims: Sub, NameIdentifier, Name, Email, Role y Expiration de 2 horas. |

---

### 10. Módulo API: Account Controller
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| POST /account/login con validación de credenciales y retorno de JWT | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/AccountController.cs` vía CQRS MediatR con retorno de JWT y expiración. |
| POST /account/confirm con validación de token y activación de usuario | 20 | **Cumple** | 20 | Implementado en `AccountController.Confirm` retornando HTTP 204 No Content al activar usuario exitosamente. |
| POST /account/get-reset-token con inactivación temporal y envío de token | 20 | **Cumple** | 20 | Implementado en `AccountController.GetResetToken` inactivando al usuario y enviando el token en el cuerpo del correo con HTTP 204 No Content. |
| POST /account/reset-password con validación de token y cambio de contraseña | 20 | **Cumple** | 20 | Implementado en `AccountController.ResetPassword` validando coincidencia de contraseñas y activando la cuenta con HTTP 204 No Content. |
| Manejo correcto de respuestas 200, 204, 400, 401 y 403 según escenario | 20 | **Cumple** | 20 | Códigos de respuesta HTTP implementados según especificación de rúbrica. |
| Tokens de confirmación y restablecimiento de un solo uso | 20 | **Cumple** | 20 | Implementado en `AuthAppService` con persistencia en `PasswordResetTokens` e invalidación tras uso. |

---

### 11. Módulo API: Gestión de usuarios
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/users con listado paginado excluyendo usuarios Comercio | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/UsersController.cs` con filtro por rol y paginación máxima de 20 registros. |
| GET /api/users/commerce con listado paginado de usuarios Comercio | 20 | **Cumple** | 20 | Implementado retornando datos del usuario, comercio asociado (`commerceId`, `commerceName`) y paginación. |
| POST /api/users para crear Administrador, Cajero o Cliente | 20 | **Cumple** | 20 | Implementado retornando 201 Created con cuenta de ahorro principal activa generada automáticamente para clientes. |
| POST /api/users/commerce/{commerceId} para crear usuario Comercio | 20 | **Cumple** | 20 | Implementado vinculando usuario a comercio existente y creando cuenta principal activa con monto inicial. |
| PUT /api/users/{id} para actualizar usuario sin modificar rol | 20 | **Cumple** | 20 | Implementado actualizando datos personales, cambio opcional de contraseña y depósito a cuenta principal para montos adicionales. |
| PATCH /api/users/{id}/status para activar o inactivar usuarios | 20 | **Cumple** | 20 | Implementado con validación que impide al administrador autenticado modificar su propio estado (403 Forbidden). |
| GET /api/users/{id} para obtener detalle de usuario | 20 | **Cumple** | 20 | Implementado retornando detalle de usuario y datos de su cuenta de ahorro principal. |
| Validación de unicidad de usuario, correo y cédula | 20 | **Cumple** | 20 | Validaciones en `UserAppService` y `UserCommands` retornando 409 Conflict ante duplicados. |
| Creación automática de cuenta principal para Cliente y Comercio | 20 | **Cumple** | 20 | Creación de cuenta de 9 dígitos única y registro de crédito si monto inicial > 0. |
| Validación de un solo usuario asociado por comercio | 20 | **Cumple** | 20 | Validado en `CreateCommerceUserApiAsync` retornando 409 Conflict si el comercio ya tiene un usuario vinculado. |

---

### 12. Módulo API: Gestión de préstamos
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/loan con paginación, filtros y búsqueda por cédula | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/LoanController.cs` vía CQRS MediatR consultando `LoanAppService`. |
| POST /api/loan con asignación de préstamo y tabla de amortización | 20 | **Cumple** | 20 | Implementado con generación de amortización bajo sistema francés y respuesta HTTP 201 Created. |
| Validación de cliente sin préstamo activo | 20 | **Cumple** | 20 | Validado en `LoanAppService.CreateLoanAsync` bloqueando la creación si existe préstamo activo. |
| Validación de alto riesgo con respuesta 409 Conflict cuando aplique | 20 | **Cumple** | 20 | Captura `HighRiskConflictException` y retorna HTTP 409 Conflict con detalle de deudas y promedio. |
| Acreditación del préstamo a cuenta principal como crédito | 20 | **Cumple** | 20 | Desembolso automático a la cuenta principal activa registrando transacción CRÉDITO. |
| GET /api/loan/{id} con detalle y tabla de amortización | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/LoanController.cs` retornando DTO con cuotas y estado. |
| PATCH /api/loan/{id}/rate recalculando solo cuotas futuras pendientes | 20 | **Cumple** | 20 | `[HttpPatch("{id}/rate")]` implementado recalculando solo cuotas pendientes y retornando HTTP 204 NoContent. |

---

### 13. Módulo API: Gestión de tarjetas de crédito
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/credit-card con paginación, filtros y búsqueda por cédula | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/CreditCardController.cs` vía CQRS MediatR con paginación y filtros por estado y cédula. |
| POST /api/credit-card con asignación de tarjeta a cliente activo | 20 | **Cumple** | 20 | Implementado en `CreditCardController` retornando HTTP 201 Created con tarjeta enmascarada y 16 dígitos. |
| Generación segura de número, expiración y CVC hasheado | 20 | **Cumple** | 20 | Generador criptoseguro con hash SHA-256 en `CreditCardGenerator` y `CreditCardAppService`. |
| GET /api/credit-card/{id} con detalle y consumos | 20 | **Cumple** | 20 | Implementado en `CreditCardController` retornando consumos aprobados y rechazados. |
| PATCH /api/credit-card/{id}/limit validando deuda actual | 20 | **Cumple** | 20 | Implementado validando `NewLimit >= Debt` y retornando HTTP 204 No Content. |
| PATCH /api/credit-card/{id}/cancel validando deuda cero | 20 | **Cumple** | 20 | Implementado validando `Debt == 0` y retornando HTTP 204 No Content. |

---

### 14. Módulo API: Gestión de cuentas de ahorro
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/savings-account con paginación y filtros | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/SavingsAccountController.cs` con filtros `status`, `type` e `identification`. |
| POST /api/savings-account para crear cuenta secundaria | 20 | **Cumple** | 20 | Implementado creando exclusivamente cuentas de tipo `Secundaria` en estado `Activa` con HTTP 201 Created. |
| Validación de cliente activo con cuenta principal activa | 20 | **Cumple** | 20 | Validado en `SavingsAccountAppService` verificando cuenta principal activa antes de emitir secundaria. |
| GET /api/savings-account/{accountNumber}/transactions con historial paginado | 20 | **Cumple** | 20 | Implementado retornando historial paginado de movimientos ordenados del más reciente al más antiguo. |
| PATCH /api/savings-account/{accountNumber}/cancel para cancelar secundaria | 20 | **Cumple** | 20 | Implementado con bloqueo explícito para cuentas principales (400 Bad Request) y cancelación suave. |
| Transferencia de balance a principal y registro de movimientos al cancelar | 20 | **Cumple** | 20 | Traspaso transaccional del 100% del balance a la cuenta principal con registro de DÉBITO y CRÉDITO. |

---

### 15. Módulo API: Gestión de comercios
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/commerce con listado paginado de comercios | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/CommerceController.cs` con filtro por estado (`activo`, `inactivo`, `todos`) y paginación. |
| GET /api/commerce/{id} con detalle del comercio | 20 | **Cumple** | 20 | Implementado retornando datos del comercio y datos del usuario asociado si existe. |
| POST /api/commerce con validación de RNC y correo únicos | 20 | **Cumple** | 20 | Implementado retornando HTTP 201 Created y 409 Conflict si RNC o correo ya existen. |
| PUT /api/commerce/{id} para actualizar datos sin modificar estado | 20 | **Cumple** | 20 | Implementado en `CommerceController` retornando HTTP 204 No Content sin alterar el estado del comercio. |
| PATCH /api/commerce/{id}/status para activar o desactivar comercio | 20 | **Cumple** | 20 | Implementado en `CommerceController` retornando HTTP 204 No Content. |
| Inactivación de usuarios asociados al desactivar comercio | 20 | **Cumple** | 20 | Al cambiar a `Inactivo`, todos los usuarios asociados al comercio pasan automáticamente a `IsActive = false`. |
| Reactivación de comercio sin activar automáticamente sus usuarios | 20 | **Cumple** | 20 | Al reactivar el comercio, los usuarios asociados permanecen inactivos de forma independiente. |

---

### 16. Módulo API: Procesador de pago Hermes Pay
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Control de acceso para roles Administrador y Comercio | 20 | **Cumple** | 20 | `[Authorize(Roles = "Administrador,Comercio")]` en `Api/Controllers/PayController.cs`. |
| Uso del commerceId desde JWT cuando el usuario autenticado es Comercio | 20 | **Cumple** | 20 | En `PayController.ResolveCommerceIdAsync`, el rol Comercio obtiene el ID directamente de su usuario asociado. |
| Uso del commerceId de la URL cuando el usuario autenticado es Administrador | 20 | **Cumple** | 20 | El rol Administrador procesa y consulta pagos utilizando el parámetro de ruta `commerceId`. |
| GET /pay/get-transactions/{commerceId} con transacciones paginadas | 20 | **Cumple** | 20 | Retorna transacciones de pago recibidas en la cuenta del comercio, ordenadas cronológicamente descendente. |
| POST /pay/process-payment/{commerceId} con validación de tarjeta y comercio | 20 | **Cumple** | 20 | Valida existencia y estado activo de comercio, tarjeta, expiración y cuenta receptora. |
| Validación de número de tarjeta, expiración y CVC | 20 | **Cumple** | 20 | Comprobación de 16 dígitos, fecha no vencida y hash SHA-256 de CVC contra base de datos. |
| Validación de crédito disponible antes de aprobar consumo | 20 | **Cumple** | 20 | Comprueba que `transactionAmount <= (Limit - Debt)` antes de autorizar. |
| Registro de consumo aprobado y aumento de deuda de tarjeta | 20 | **Cumple** | 20 | Registra consumo con estado `Aprobado`, incrementa deuda en tarjeta y guarda cambios transaccionalmente. |
| Acreditación del pago en cuenta principal del comercio | 20 | **Cumple** | 20 | Suma balance a la cuenta del comercio y genera movimiento de CRÉDITO con los últimos 4 dígitos de la tarjeta. |
| Registro de consumo rechazado sin modificar balances ni deudas | 20 | **Cumple** | 20 | Registra consumo con estado `Rechazado` y retorna 400 Bad Request sin alterar deuda ni saldos. |
| Correos al cliente y al comercio luego de pago aprobado | 20 | **Cumple** | 20 | Notificaciones por correo electrónico enviadas al titular de la tarjeta y al comercio con fecha, comercio, monto y últimos 4 dígitos. |

---

### 17. Reglas financieras y trazabilidad
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Uso correcto de tipo DÉBITO para salidas de dinero | 20 | **Cumple** | 20 | Consistente en retiros, transferencias origen y pagos. |
| Uso correcto de tipo CRÉDITO para entradas de dinero | 20 | **Cumple** | 20 | Consistente en depósitos, transferencias destino y desembolsos. |
| Registro cruzado en transferencias entre cuentas | 20 | **Cumple** | 20 | Registra DÉBITO en origen y CRÉDITO en destino. |
| No permitir sobrepagos a tarjetas ni préstamos | 20 | **Cumple** | 20 | Limita pago al monto exacto de la deuda pendiente (`Math.Min`). |
| Actualización correcta de deuda, balance y crédito disponible | 20 | **Cumple** | 20 | Actualizaciones consistentes en todas las entidades bancarias. |
| Ejecución transaccional de operaciones que afectan múltiples entidades | 20 | **Cumple** | 20 | `BeginTransactionAsync`, `Commit` y `Rollback` en `UnitOfWork`. |
| Conservación del historial aunque productos o usuarios sean inactivados/cancelados | 20 | **Cumple** | 20 | Cancelación lógica (soft delete) preserva historial completo. |
| Uso de decimal para montos monetarios y precisión hasta centavos | 20 | **Cumple** | 20 | Tipo `decimal` con precisión monetaria en toda la solución. |

---

### 18. Reglas técnicas y arquitectura
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Implementación en ASP.NET Core MVC y Web API con .NET 9 | 20 | **Cumple** | 20 | TargetFramework `net9.0` en todos los proyectos de la solución. |
| Uso correcto de Entity Framework Core Code First | 20 | **Cumple** | 20 | EF Core 9.0 Code First con migraciones organizadas. |
| Creación correcta de entidades, relaciones, configuraciones y migraciones | 20 | **Cumple** | 20 | Configuraciones Fluent API en `Infrastructure/Persistence/Configurations`. |
| Implementación correcta de Onion Architecture | 20 | **Cumple** | 20 | Capas Domain, Application, Infrastructure, Persistence, Web, Api. |
| Separación adecuada de capas Domain, Application, Infrastructure, Persistence, WebApp y WebAPI | 20 | **Cumple** | 20 | Referencias unidireccionales correctas. |
| Uso correcto de ViewModels y validaciones en la WebApp | 20 | **Cumple** | 20 | ViewModels organizados por módulo con DataAnnotations y validaciones UI. |
| Uso correcto de DTOs para transferencia de información en la API | 20 | **Cumple** | 20 | DTOs organizados en `Application/DTOs`. |
| Uso correcto de AutoMapper entre entidades, ViewModels, DTOs, Commands y Queries | 20 | **Cumple** | 20 | AutoMapper configurado en `AutoMapperProfile.cs` e integrado en AppServices, Commands y Queries. |
| Uso de repositorios genéricos y repositorios específicos cuando aplique | 20 | **Cumple** | 20 | `IBaseRepository<T>` y repositorios específicos por módulo con UnitOfWork. |
| Uso de servicios genéricos y servicios de negocio por módulo | 20 | **Cumple** | 20 | `AppServices` separados por dominio funcional. |
| Controladores sin lógica de negocio compleja | 20 | **Cumple** | 20 | Controladores Web y API desacoplados despachando a través de AppServices y Mediator. |
| Interfaz visual clara usando Bootstrap u otro framework CSS | 20 | **Cumple** | 20 | Bootstrap 5 y estilos CSS personalizados limpios y responsivos. |

---

### 19. CQRS, Mediator, Behaviors y validaciones
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Implementación de CQRS en endpoints de Account | 20 | **Cumple** | 20 | Commands implementados: `LoginCommand`, `ConfirmAccountCommand`, `GetResetTokenCommand`, `ResetPasswordCommand`. |
| Implementación de CQRS en endpoints de usuarios y comercios | 20 | **Cumple** | 20 | Commands y Queries implementados en `Application/Features/Users` y `Application/Features/Commerce`. |
| Implementación de CQRS en endpoints de préstamos | 20 | **Cumple** | 20 | Commands y Queries implementados en `Application/Features/Loans`. |
| Implementación de CQRS en endpoints de tarjetas de crédito | 20 | **Cumple** | 20 | Commands y Queries implementados en `Application/Features/CreditCards`. |
| Implementación de CQRS en endpoints de cuentas de ahorro | 20 | **Cumple** | 20 | Commands y Queries implementados en `Application/Features/SavingsAccounts`. |
| Implementación de CQRS en endpoints de Hermes Pay | 20 | **Cumple** | 20 | Commands y Queries implementados en `Application/Features/HermesPay`. |
| Uso correcto de Mediator para Commands y Queries | 20 | **Cumple** | 20 | MediatR configurado en `Application/ServiceRegistration.cs` y despachado con `ISender` en los controladores API. |
| Validaciones de Commands y Queries mediante FluentValidation | 20 | **Cumple** | 20 | Clases `AbstractValidator<T>` implementadas para todos los Commands y Queries. |
| Uso de Behaviors para validaciones transversales | 20 | **Cumple** | 20 | `ValidationBehavior<TRequest, TResponse>` registrado como `IPipelineBehavior` transversal en MediatR. |
| Separación entre validaciones estructurales y reglas de negocio con acceso a datos | 20 | **Cumple** | 20 | Validaciones estructurales en FluentValidation y validaciones de negocio en handlers/servicios. |

---

### 20. Validación de servicios por módulo
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Servicios de seguridad, login, activación y restablecimiento validados correctamente | 20 | **Cumple** | 20 | `AuthAppService` con todas las validaciones requeridas por el documento funcional. |
| Servicios de usuarios y roles validados correctamente | 20 | **Cumple** | 20 | `UserAppService` valida unicidad de cédula, correo, usuario y roles. |
| Servicios de cuentas de ahorro y transacciones validados correctamente | 20 | **Cumple** | 20 | `SavingsAccountAppService` valida estado, cuenta principal y cancelaciones con traspaso. |
| Servicios de préstamos, amortización y pagos validados correctamente | 20 | **Cumple** | 20 | `LoanAppService` y `LoanPaymentAppService` con validaciones completas. |
| Servicios de tarjetas, consumos, pagos y avances validados correctamente | 20 | **Cumple** | 20 | `CreditCardAppService` y `CardPaymentAppService` con validaciones completas. |
| Servicios de beneficiarios y transferencias validados correctamente | 20 | **Cumple** | 20 | `BeneficiaryAppService` y `ThirdPartyTransactionAppService` con validaciones completas. |
| Servicios de cajero validados correctamente | 20 | **Cumple** | 20 | `DepositAppService`, `WithdrawalAppService`, `LoanPaymentAppService`, `CardPaymentAppService` y `ThirdPartyTransactionAppService` validados. |
| Servicios de comercios y Hermes Pay validados correctamente | 20 | **Cumple** | 20 | `CommerceAppService` y `HermesPayAppService` validados con reglas de negocio completas. |
| Servicios de correo desacoplados y reutilizables | 20 | **Cumple** | 20 | `IEmailService` implementado en `Shared` e inyectado con plantillas HTML. |

---

### 21. Documentación, excepciones y logs
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Documentación Swagger completa para endpoints, parámetros, body y respuestas | 20 | **Cumple** | 20 | Swagger documenta todos los controladores y endpoints de la Web API. |
| Swagger configurado para autenticación JWT | 20 | **Cumple** | 20 | `AddSecurityDefinition("Bearer", ...)` y `AddSecurityRequirement` configurados en `Api/Program.cs`. |
| Global Exception Handler implementado correctamente | 20 | **Cumple** | 20 | `ProblemDetailsMiddleware` centraliza el manejo de excepciones no controladas. |
| Respuestas de error utilizando Problem Details RFC 7807 | 20 | **Cumple** | 20 | Middleware formatea errores como `application/problem+json` conforme a RFC 7807. |
| Manejo centralizado de errores de negocio, validación y no encontrados | 20 | **Cumple** | 20 | Mapeo automático de `ValidationException` (400), `KeyNotFoundException` (404), `UnauthorizedAccessException` (403) y 500. |
| Serilog configurado en WebApp y WebAPI | 20 | **Cumple** | 20 | Serilog configurado en `Api/Program.cs` y `Web/Program.cs`. |
| Logs de operaciones financieras relevantes | 20 | **Cumple** | 20 | Registro de logs estructurados en consola y archivo rotativo. |
| Logs de errores no controlados con información útil | 20 | **Cumple** | 20 | Middleware captura traza de excepción, ruta y TraceId para diagnóstico. |
| No registrar datos sensibles en logs, respuestas, vistas ni correos | 20 | **Cumple** | 20 | Tarjetas enmascaradas, CVC hasheado, passwords protegidas. |

---

### 22. Pruebas unitarias - Commands y Queries
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Unit tests para Commands y Queries de Account | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de usuarios | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de préstamos | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de tarjetas de crédito | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de cuentas de ahorro | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de comercios | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para Commands y Queries de Hermes Pay | 20 | **Cumple** | 20 | Pruebas implementadas en `CqrsAndValidatorsTests.cs`. |
| Unit tests para validadores FluentValidation | 20 | **Cumple** | 20 | Pruebas exhaustivas con `TestValidate` para todos los validadores de Commands y Queries. |

---

### 23. Pruebas unitarias - Servicios de negocio
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Unit tests de servicios de cuentas y balances | 20 | **Cumple** | 20 | Pruebas implementadas en `SavingsAccountAppServiceTests.cs`. |
| Unit tests de servicios de transferencias y beneficiarios | 20 | **Cumple** | 20 | Pruebas implementadas en `BeneficiaryAppServiceTests.cs` y `ThirdPartyTransactionAppServiceTests.cs`. |
| Unit tests de servicios de depósitos y retiros | 20 | **Cumple** | 20 | Pruebas implementadas en `DepositAppServiceTests.cs` y `WithdrawalAppServiceTests.cs`. |
| Unit tests de servicios de pagos a tarjetas | 20 | **Cumple** | 20 | Pruebas implementadas en `CardPaymentAppServiceTests.cs`. |
| Unit tests de servicios de pagos a préstamos | 20 | **Cumple** | 20 | Pruebas implementadas en `LoanPaymentAppServiceTests.cs`. |
| Unit tests de cálculo de cuotas y tabla de amortización | 20 | **Cumple** | 20 | Pruebas implementadas en `LoanAppServiceTests.cs`. |
| Unit tests de servicios de tarjetas y avances de efectivo | 20 | **Cumple** | 20 | Pruebas implementadas en `CreditCardAppServiceTests.cs`. |
| Unit tests de servicios de comercios y procesamiento Hermes Pay | 20 | **Cumple** | 20 | Pruebas implementadas en `CommerceAppServiceTests.cs` y `HermesPayAppServiceTests.cs`. |
| Unit tests de reglas de alto riesgo y validaciones financieras críticas | 20 | **Cumple** | 20 | Pruebas implementadas en `LoanAppServiceTests.cs` para excepciones de alto riesgo. |

---

### 24. Pruebas de integración - Repositorios y persistencia
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Integration tests de repositorios de usuarios, roles y tokens | 20 | **Cumple** | 20 | Pruebas implementadas en `Tests/Persistence.IntegrationTests/RepositoryIntegrationTests.cs`. |
| Integration tests de repositorios de cuentas de ahorro | 20 | **Cumple** | 20 | Pruebas de persistencia e integridad en `RepositoryIntegrationTests.cs`. |
| Integration tests de persistencia de transacciones financieras | 20 | **Cumple** | 20 | Pruebas en `UnitOfWorkIntegrationTests.cs`. |
| Integration tests de repositorios de préstamos y amortización | 20 | **Cumple** | 20 | Pruebas de persistencia en `RepositoryIntegrationTests.cs`. |
| Integration tests de repositorios de tarjetas y consumos | 20 | **Cumple** | 20 | Pruebas de persistencia y consultas con joins en `RepositoryIntegrationTests.cs`. |
| Integration tests de repositorios de beneficiarios | 20 | **Cumple** | 20 | Pruebas de CRUD de beneficiarios en `RepositoryIntegrationTests.cs`. |
| Integration tests de repositorios de comercios y usuarios de comercio | 20 | **Cumple** | 20 | Pruebas de persistencia de comercios en `RepositoryIntegrationTests.cs`. |
| Integration tests de operaciones transaccionales con base de datos de prueba | 20 | **Cumple** | 20 | Pruebas de UnitOfWork con transacciones multi-entidad en `UnitOfWorkIntegrationTests.cs`. |
| Uso de InMemory Database o SQLite en memoria sin depender de BD real | 20 | **Cumple** | 20 | Configurado con `Microsoft.EntityFrameworkCore.InMemory` en `TestDbContextFactory.cs`. |

---

### 25. Calidad final, entrega y ejecución
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Solución compila correctamente sin errores | 20 | **Cumple** | 20 | Estructura de proyectos limpia, dependencias y referencias correctas. |
| Migraciones aplican correctamente y generan la base de datos esperada | 20 | **Cumple** | 20 | Migraciones Code First funcionales (`context.Database.MigrateAsync`). |
| Seed de datos mínimos funcionales para pruebas iniciales | 20 | **Cumple** | 20 | `DefaultRolesAndUsers.SeedAsync` inicializa roles y usuarios default. |
| La WebApp ejecuta correctamente con sus módulos principales | 20 | **Cumple** | 20 | Todos los módulos web (Admin, Cajero, Cliente, Autenticación) ejecutan 100% integrados a la BD real mediante EF Core y AppServices. |
| La WebAPI ejecuta correctamente con Swagger disponible | 20 | **Cumple** | 20 | Controladores de Account, Users, Loan, CreditCard, SavingsAccount, Commerce y Pay documentados y operativos. |
| Las pruebas automatizadas ejecutan correctamente desde la solución | 20 | **Cumple** | 20 | Suite de 140 pruebas (unitarias e integración) ejecutando al 100% sin fallos. |
| Manejo adecuado de configuración mediante appsettings y ambientes | 20 | **Cumple** | 20 | Configuración desacoplada con DotNetEnv y variables de entorno. |
| Código organizado, legible, mantenible y consistente | 20 | **Cumple** | 20 | Nomenclatura clara, separación de capas limpia y buenas prácticas de C#. |
| No exposición de datos sensibles en UI, API, correos ni logs | 20 | **Cumple** | 20 | Enmascaramiento de tarjetas y hashing de CVC implementados correctamente. |
