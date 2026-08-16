# Evaluación Proyecto Final: Artemis Banking Pro (ABP) - Estado Actual

> **Fecha de Evaluación:** 16 de Agosto de 2026  
> **Documentos de Referencia:**  
> - [`Evaluación_Proyecto_Final_ Artemis_Banking_Pro_(ABP).md`](file:///home/briamco/projects/ArtemisPro/Evaluación_Proyecto_Final_%20Artemis_Banking_Pro_%28ABP%29.md)  
> - [`documento-funcional.md`](file:///home/briamco/projects/ArtemisPro/documento-funcional.md)  
>
> **Puntos Totales Disponibles:** **4,460 puntos** (223 criterios × 20 pts)  
> **Puntuación Obtenida:** **2,240 / 4,460 puntos**  
> **Porcentaje de Cumplimiento:** **50.22%**

---

## 📊 Resumen por Módulo

| Módulo / Sección | Criterios | Valor Total | Estado General | Puntos Obtenidos | % Cumplimiento |
|---|:---:|:---:|:---:|:---:|:---:|
| 1. Funcionalidades generales y seguridad WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 2. Home del administrador | 8 | 160 | **Parcial** | 100 | 62.5% |
| 3. Gestión de usuarios WebApp | 9 | 180 | **Parcial** | 100 | 55.6% |
| 4. Gestión de préstamos WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 5. Gestión de tarjetas de crédito WebApp | 10 | 200 | **Cumple** | 200 | 100.0% |
| 6. Gestión de cuentas de ahorro WebApp | 8 | 160 | **Cumple** | 160 | 100.0% |
| 7. Funcionalidades del cliente | 11 | 220 | **Parcial** | 110 | 50.0% |
| 8. Funcionalidades del cajero | 10 | 200 | **Parcial** | 130 | 65.0% |
| 9. Seguridad general de la Web API | 8 | 160 | **Cumple** | 140 | 87.5% |
| 10. Módulo API: Account Controller | 6 | 120 | **Parcial** | 40 | 33.3% |
| 11. Módulo API: Gestión de usuarios | 10 | 200 | **No cumple** | 20 | 10.0% |
| 12. Módulo API: Gestión de préstamos | 7 | 140 | **Cumple** | 110 | 78.6% |
| 13. Módulo API: Gestión de tarjetas de crédito | 6 | 120 | **No cumple** | 10 | 8.3% |
| 14. Módulo API: Gestión de cuentas de ahorro | 6 | 120 | **No cumple** | 20 | 16.7% |
| 15. Módulo API: Gestión de comercios | 7 | 140 | **No cumple** | 0 | 0.0% |
| 16. Módulo API: Procesador de pago Hermes Pay | 11 | 220 | **No cumple** | 0 | 0.0% |
| 17. Reglas financieras y trazabilidad | 8 | 160 | **Cumple** | 160 | 100.0% |
| 18. Reglas técnicas y arquitectura | 11 | 220 | **Cumple** | 200 | 90.9% |
| 19. CQRS, Mediator, Behaviors y validaciones | 10 | 200 | **No cumple** | 10 | 5.0% |
| 20. Validación de servicios por módulo | 9 | 180 | **Cumple** | 160 | 88.9% |
| 21. Documentación, excepciones y logs | 9 | 180 | **Parcial** | 90 | 50.0% |
| 22. Pruebas unitarias - Commands y Queries | 8 | 160 | **No cumple** | 0 | 0.0% |
| 23. Pruebas unitarias - Servicios de negocio | 9 | 180 | **Parcial** | 30 | 16.7% |
| 24. Pruebas de integración - Repositorios y persistencia | 9 | 180 | **No cumple** | 0 | 0.0% |
| 25. Calidad final, entrega y ejecución | 9 | 180 | **Cumple** | 150 | 83.3% |
| **TOTAL GENERAL** | **223** | **4,460** | **Parcial** | **2,240** | **50.22%** |

---

## 📑 Evaluación Detallada Criterio por Criterio

### 1. Funcionalidades generales y seguridad WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Login web con validación de credenciales, usuario activo y rol permitido | 20 | **Cumple** | 20 | Implementado en `AccountController` y `AuthAppService.WebLoginAsync`. |
| Redirección correcta al Home según rol autenticado | 20 | **Cumple** | 20 | Redirecciona a Admin (`Admin/Index`), Cajero (`Cashier/Index`) o Cliente (`Client/Index`). |
| Activación de cuenta mediante enlace/token de un solo uso | 20 | **Cumple** | 20 | Token en Base64Url y activación en `AuthAppService.ActivateAccountAsync`. |
| Restablecimiento de contraseña con token vigente y confirmación de contraseña | 20 | **Cumple** | 20 | Token de 30 min, validación de coincidencia y un solo uso. |
| Menú principal con opciones correspondientes según rol | 20 | **Cumple** | 20 | Menús diferenciados en layouts `_Layout.cshtml`, `_LayoutCashier.cshtml`, `_LayoutClient.cshtml`. |
| Navegación correcta entre módulos de la WebApp | 20 | **Cumple** | 20 | Rutas y enlaces de navegación consistentes. |
| Uso consistente del layout general de la aplicación | 20 | **Cumple** | 20 | Layouts estructurados y diseño Bootstrap uniforme. |
| Mensajes de validación y confirmación claros para el usuario | 20 | **Cumple** | 20 | TempData y ModelState con mensajes acordes al documento funcional. |
| Restricción de acceso directo por URL según rol | 20 | **Cumple** | 20 | Atributos `[Authorize(Roles = "...")]` y redirección a `AccessDenied`. |
| Creación por seeding de roles y usuarios por defecto activos | 20 | **Cumple** | 20 | `DefaultRolesAndUsers.SeedAsync` crea Admin, Cajero y Cliente activos por defecto. |

---

### 2. Home del Administrador
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Redirección correcta al Home del administrador luego del login | 20 | **Cumple** | 20 | Redirección directa a `Admin/Index`. |
| Menú del administrador con todos los módulos requeridos | 20 | **Cumple** | 20 | Incluye Home, Usuarios, Préstamos, Tarjetas, Cuentas y Logout. |
| Indicadores generales calculados correctamente | 20 | **Parcial** | 10 | `AdminDashboardAppService` está implementado, pero `AdminController.Index` usa datos mock para transacciones y pagos. |
| Cálculo correcto de transacciones históricas y transacciones del día | 20 | **Parcial** | 10 | En el servicio sí, pero en la vista del controlador se envían valores fijos (1250 / 42). |
| Cálculo correcto de pagos históricos y pagos del día | 20 | **Parcial** | 10 | En el servicio sí, pero en la vista del controlador se envían valores fijos (840 / 18). |
| Cálculo correcto de clientes activos e inactivos | 20 | **Cumple** | 20 | Consulta real a `_userManager.GetUsersInRoleAsync("Cliente")`. |
| Cálculo correcto de productos financieros activos | 20 | **Parcial** | 10 | Cuentas y préstamos se calculan de BD, tarjetas de lista mock `_dummyCards`. |
| Cálculo correcto de deuda promedio por cliente | 20 | **Parcial** | 10 | Calcula préstamos reales de BD pero tarjetas de mock. |

---

### 3. Gestión de usuarios WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado paginado de usuarios y filtro por rol | 20 | **Parcial** | 10 | La UI y el filtro existen, pero opera sobre `_dummyUsers` estático en el controlador. |
| Creación de usuarios Administrador, Cajero y Cliente | 20 | **Parcial** | 10 | Vista y validaciones completas, pero guarda en lista en memoria `_dummyUsers`. |
| Validación de usuario, correo y cédula únicos | 20 | **Parcial** | 10 | Implementado en `UserAppService`, pero no conectado al controlador web. |
| Validación de contraseña y confirmación de contraseña | 20 | **Cumple** | 20 | Validaciones en ViewModels (`CreateUserViewModel` / `EditUserViewModel`). |
| Creación de cliente con cuenta de ahorro principal automática | 20 | **Parcial** | 10 | Lógica lista en `UserAppService.CreateUserAsync`, pero UI usa lista mock. |
| Registro de monto inicial como crédito cuando aplique | 20 | **Parcial** | 10 | Lógica lista en `UserAppService`, pero UI usa lista mock. |
| Envío de correo de activación al crear usuario | 20 | **Parcial** | 10 | Lógica en `UserAppService` / `AuthAppService`, pero UI usa lista mock. |
| Edición de usuario sin permitir modificar el rol | 20 | **Parcial** | 10 | Vista no permite editar rol, pero edita lista mock `_dummyUsers`. |
| Manejo de monto adicional para clientes y registro de crédito | 20 | **Parcial** | 10 | Implementado en `UserAppService`, pero UI usa lista mock. |
| Activación e inactivación de usuarios con bloqueo de auto-modificación | 20 | **Parcial** | 10 | Bloquea auto-modificación pero actualiza lista mock. |

---

### 4. Gestión de préstamos WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de préstamos con paginación, filtros por estado y búsqueda por cédula | 20 | **Cumple** | 20 | Totalmente integrado con `_loanAppService.GetLoansAsync` y BD real. |
| Asignación de préstamo solo a cliente activo sin préstamo activo | 20 | **Cumple** | 20 | Filtros en asistente paso 1 y validación en `LoanAppService`. |
| Validación de cliente de alto riesgo según deuda promedio | 20 | **Cumple** | 20 | Vista `RiskAlert` y cálculo de umbral promedio contra deuda actual y proyectada. |
| Cálculo correcto de cuota bajo sistema francés | 20 | **Cumple** | 20 | Algoritmo francés implementado en `LoanAppService`. |
| Generación correcta de tabla de amortización | 20 | **Cumple** | 20 | Genera cuotas con capital, interés y balance remanente. |
| Generación de número de préstamo único de 9 dígitos como texto | 20 | **Cumple** | 20 | Generador único de 9 dígitos verificado contra préstamos y cuentas. |
| Desembolso del préstamo a la cuenta principal del cliente | 20 | **Cumple** | 20 | Acredita balance en cuenta principal activa. |
| Registro del desembolso como transacción de tipo crédito | 20 | **Cumple** | 20 | Transacción CRÉDITO "Desembolso de Préstamo" registrada. |
| Detalle de préstamo con tabla de amortización y estado de cuotas | 20 | **Cumple** | 20 | Vista `LoanDetails` muestra tabla y estados. |
| Edición de tasa recalculando solo cuotas futuras pendientes | 20 | **Cumple** | 20 | `EditLoanRate` recalcula cuotas futuras pendientes. |

---

### 5. Gestión de tarjetas de crédito WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de tarjetas con paginación, filtros y búsqueda por cédula | 20 | **Cumple** | 20 | Conectado a `_creditCardService.GetCreditCardsAsync`. |
| Asignación de tarjeta a cliente activo | 20 | **Cumple** | 20 | Wizard de 2 pasos con validaciones en servicio. |
| Generación de número de tarjeta único de 16 dígitos | 20 | **Cumple** | 20 | `CreditCardGenerator.GenerateCardNumber` genera 16 dígitos. |
| Generación de fecha de expiración y CVC | 20 | **Cumple** | 20 | Expiración a 3 años y CVC de 3 dígitos. |
| Almacenamiento del CVC como hash y no como texto plano | 20 | **Cumple** | 20 | SHA256 CvcHash almacenado en entidad `CreditCard`. |
| Visualización de tarjeta enmascarada y últimos cuatro dígitos | 20 | **Cumple** | 20 | Formato `**** **** **** 1234`. |
| Detalle de tarjeta con consumos aprobados y rechazados | 20 | **Cumple** | 20 | Vista `CreditCardDetails` consulta transacciones de tarjeta. |
| Edición de límite validando que no sea menor a la deuda actual | 20 | **Cumple** | 20 | Valida `NewLimit >= Debt` en `EditCreditCardLimit`. |
| Cancelación de tarjeta únicamente si no tiene deuda pendiente | 20 | **Cumple** | 20 | Valida `Debt == 0` antes de cancelar. |
| Notificaciones por correo al asignar o modificar tarjeta | 20 | **Cumple** | 20 | Envía correos con `IEmailService`. |

---

### 6. Gestión de cuentas de ahorro WebApp
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Listado de cuentas con paginación, filtros por estado, tipo y cédula | 20 | **Cumple** | 20 | Conectado a `_savingsAccountAppService.GetSavingsAccountsAsync`. |
| Asignación de cuenta secundaria solo a cliente activo | 20 | **Cumple** | 20 | Asistente de selección y validación en servicio. |
| Validación de existencia de cuenta principal activa antes de crear secundaria | 20 | **Cumple** | 20 | Verificado en paso 1 y en `CreateSavingsAccountAsync`. |
| Generación de número de cuenta único de 9 dígitos como texto | 20 | **Cumple** | 20 | Generador único de 9 dígitos verificado en BD. |
| Registro del balance inicial como crédito cuando aplique | 20 | **Cumple** | 20 | Registra transacción CRÉDITO si `InitialBalance > 0`. |
| Detalle de cuenta con historial de transacciones | 20 | **Cumple** | 20 | Vista `SavingsAccountDetails` lista transacciones. |
| Cancelación exclusiva de cuentas secundarias activas | 20 | **Cumple** | 20 | Bloquea cancelación de cuentas principales. |
| Transferencia automática del balance a cuenta principal al cancelar secundaria | 20 | **Cumple** | 20 | `CancelSavingsAccountAsync` transfiere fondos a cuenta principal. |
| Registro cruzado de débito y crédito al cancelar cuenta con balance | 20 | **Cumple** | 20 | Registra DÉBITO en secundaria y CRÉDITO en principal. |
| Bloqueo de operaciones sobre cuentas canceladas | 20 | **Cumple** | 20 | Validado en servicios de negocio. |

---

### 7. Funcionalidades del cliente
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Home del cliente con listado de productos financieros activos | 20 | **Parcial** | 10 | Vista construida, pero `ClientController.Index` usa datos mock en memoria. |
| Visualización de detalles de cuentas, préstamos y tarjetas | 20 | **Parcial** | 10 | Vistas completas, pero controladores devuelven listas mock. |
| Gestión de beneficiarios con validación de cuenta activa y no propia | 20 | **Parcial** | 10 | Validaciones existen en servicio, pero controlador web opera sobre lista mock. |
| Eliminación de beneficiarios sin afectar historial ni cuentas | 20 | **Parcial** | 10 | Vista y flujo existen, pero sobre lista mock. |
| Transacción Express con validación de cuenta destino y fondos | 20 | **Parcial** | 10 | Flujo y confirmación existen, pero operan sobre listas mock en controlador. |
| Pago de tarjeta de crédito desde cuenta propia sin permitir sobrepago | 20 | **Parcial** | 10 | Lógica lista en `PaymentAppService`, pero UI usa lista mock. |
| Pago de préstamo aplicando cuotas en orden de antigüedad | 20 | **Parcial** | 10 | Lógica lista en `PaymentAppService`, pero UI usa lista mock. |
| Transacción a beneficiarios con registro de débito y crédito | 20 | **Parcial** | 10 | Lógica lista en `TransferAppService`, pero UI usa lista mock. |
| Avance de efectivo con interés del 6.25% y validación de crédito disponible | 20 | **Parcial** | 10 | La lógica del 6.25% está escrita en el controlador, pero sobre lista mock. |
| Transferencia entre cuentas propias con validación de origen y destino distintos | 20 | **Parcial** | 10 | Lógica lista en `TransferAppService`, pero UI usa lista mock. |
| Correos de notificación y registros de historial en operaciones del cliente | 20 | **Parcial** | 10 | Servicios envían correos, pero no son invocados por el portal web del cliente. |

---

### 8. Funcionalidades del cajero
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Home del cajero con indicadores del día calculados correctamente | 20 | **Parcial** | 10 | Vista existe, pero `CashierController.Index` tiene contadores fijos (20, 10, 15, 45). |
| Depósito a cuenta de ahorro con validaciones y registro como crédito | 20 | **Parcial** | 10 | `DepositAppService` implementado, pero `CashierController.Deposit` usa lista mock. |
| Retiro desde cuenta de ahorro con validación de fondos y registro como débito | 20 | **Parcial** | 10 | `WithdrawalAppService` implementado, pero `CashierController.Withdrawal` usa lista mock. |
| Pago a tarjeta de crédito desde cuenta de ahorro con validación de deuda | 20 | **Cumple** | 20 | `CashierController.PayCreditCard` está conectado a `_cardPaymentService` y BD real. |
| Pago a préstamo desde cuenta de ahorro aplicando cuotas en orden | 20 | **Parcial** | 10 | `PaymentAppService` implementado, pero `CashierController.PayLoan` usa lista mock. |
| Transacciones a cuentas de terceros con registro cruzado de débito y crédito | 20 | **Parcial** | 10 | Vista y validaciones existen, pero opera sobre lista mock. |
| Registro de intentos rechazados cuando aplique sin afectar balances | 20 | **Parcial** | 10 | Implementado en servicios de backend, pero no conectado a todos los endpoints de cajero. |
| Asociación de operaciones al cajero autenticado | 20 | **Cumple** | 20 | Guarda `teller.Id` en `PerformedById` en transacciones reales. |
| Confirmaciones previas a operaciones financieras del cajero | 20 | **Cumple** | 20 | Pantallas de confirmación implementadas para todas las operaciones. |
| Correos de notificación al cliente emisor y receptor cuando corresponda | 20 | **Parcial** | 10 | En pagos a tarjetas sí envía; en el resto falta conectar el servicio real a la UI. |

---

### 9. Seguridad general de la Web API
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Autenticación JWT configurada correctamente | 20 | **Cumple** | 20 | `AddAuthentication` con `JwtBearerDefaults` en `Api/Program.cs`. |
| Autorización por roles en endpoints protegidos | 20 | **Cumple** | 20 | `[Authorize(Roles = "...")]` en controladores API. |
| Respuesta 401 para token ausente, inválido o expirado | 20 | **Cumple** | 20 | Gestionado por middleware de autenticación JWT. |
| Respuesta 403 para usuario autenticado sin permisos | 20 | **Cumple** | 20 | Gestionado por middleware de autorización. |
| Separación correcta de roles Administrador y Comercio en la API | 20 | **Parcial** | 10 | Roles existen en seeding, pero endpoints de comercio no están creados. |
| Endpoints públicos de Account disponibles sin JWT cuando corresponda | 20 | **Cumple** | 20 | `AccountController` en Api permite login sin token. |
| Usuarios API creados inactivos hasta confirmación o restablecimiento | 20 | **Parcial** | 10 | `isApiUser` activa la cuenta directamente en `AuthAppService`, faltando flujo inactivo. |
| JWT con identificador de usuario, nombre de usuario, rol y expiración | 20 | **Cumple** | 20 | Claims: NameIdentifier, Name, Email, Role, Expiration. |

---

### 10. Módulo API: Account Controller
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| POST /account/login con validación de credenciales y retorno de JWT | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/AccountController.cs`. |
| POST /account/confirm con validación de token y activación de usuario | 20 | **No cumple** | 0 | Endpoint no existe en la API. |
| POST /account/get-reset-token con inactivación temporal y envío de token | 20 | **No cumple** | 0 | Endpoint no existe en la API. |
| POST /account/reset-password con validación de token y cambio de contraseña | 20 | **No cumple** | 0 | Endpoint no existe en la API. |
| Manejo correcto de respuestas 200, 204, 400, 401 y 403 según escenario | 20 | **Parcial** | 10 | Solo implementado para el login (200, 400, 401). |
| Tokens de confirmación y restablecimiento de un solo uso | 20 | **Parcial** | 10 | Lógica lista en `AuthAppService`, pero falta exponer endpoints en API. |

---

### 11. Módulo API: Gestión de usuarios
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/users con listado paginado excluyendo usuarios Comercio | 20 | **No cumple** | 0 | No existe `UsersController` en el proyecto Api. |
| GET /api/users/commerce con listado paginado de usuarios Comercio | 20 | **No cumple** | 0 | Endpoint no implementado. |
| POST /api/users para crear Administrador, Cajero o Cliente | 20 | **No cumple** | 0 | Endpoint no implementado. |
| POST /api/users/commerce/{commerceId} para crear usuario Comercio | 20 | **No cumple** | 0 | Endpoint no implementado. |
| PUT /api/users/{id} para actualizar usuario sin modificar rol | 20 | **No cumple** | 0 | Endpoint no implementado. |
| PATCH /api/users/{id}/status para activar o inactivar usuarios | 20 | **No cumple** | 0 | Endpoint no implementado. |
| GET /api/users/{id} para obtener detalle de usuario | 20 | **No cumple** | 0 | Endpoint no implementado. |
| Validación de unicidad de usuario, correo y cédula | 20 | **Parcial** | 10 | Lógica implementada en `UserAppService`, pero endpoint API ausente. |
| Creación automática de cuenta principal para Cliente y Comercio | 20 | **Parcial** | 10 | Implementado para clientes en `UserAppService`, no expuesto en API. |
| Validación de un solo usuario asociado por comercio | 20 | **No cumple** | 0 | Lógica y endpoints de comercio no implementados. |

---

### 12. Módulo API: Gestión de préstamos
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/loan con paginación, filtros y búsqueda por cédula | 20 | **Parcial** | 10 | `GET api/loan` existe con filtros pero sin paginación explícita (page/pageSize). |
| POST /api/loan con asignación de préstamo y tabla de amortización | 20 | **Cumple** | 20 | `POST api/loan` crea préstamo y genera amortización. |
| Validación de cliente sin préstamo activo | 20 | **Cumple** | 20 | Validado en `LoanAppService`. |
| Validación de alto riesgo con respuesta 409 Conflict cuando aplique | 20 | **Parcial** | 10 | Lógica de riesgo existe, pero el controlador retorna `BadRequest` en vez de `Conflict (409)`. |
| Acreditación del préstamo a cuenta principal como crédito | 20 | **Cumple** | 20 | Desembolso automático a cuenta principal. |
| GET /api/loan/{id} con detalle y tabla de amortización | 20 | **Cumple** | 20 | Implementado en `Api/Controllers/LoanController.cs`. |
| PATCH /api/loan/{id}/rate recalculando solo cuotas futuras pendientes | 20 | **Parcial** | 10 | Recalcula cuotas pendientes, pero usa verbo `PUT` en vez de `PATCH`. |

---

### 13. Módulo API: Gestión de tarjetas de crédito
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/credit-card con paginación, filtros y búsqueda por cédula | 20 | **No cumple** | 0 | No existe `CreditCardController` en Api. |
| POST /api/credit-card con asignación de tarjeta a cliente activo | 20 | **No cumple** | 0 | Endpoint ausente. |
| Generación segura de número, expiración y CVC hasheado | 20 | **Parcial** | 10 | Lógica lista en `CreditCardAppService`, falta controlador API. |
| GET /api/credit-card/{id} con detalle y consumos | 20 | **No cumple** | 0 | Endpoint ausente. |
| PATCH /api/credit-card/{id}/limit validando deuda actual | 20 | **No cumple** | 0 | Endpoint ausente. |
| PATCH /api/credit-card/{id}/cancel validando deuda cero | 20 | **No cumple** | 0 | Endpoint ausente. |

---

### 14. Módulo API: Gestión de cuentas de ahorro
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/savings-account con paginación y filtros | 20 | **No cumple** | 0 | No existe `SavingsAccountController` en Api. |
| POST /api/savings-account para crear cuenta secundaria | 20 | **No cumple** | 0 | Endpoint ausente. |
| Validación de cliente activo con cuenta principal activa | 20 | **Parcial** | 10 | Validado en `SavingsAccountAppService`, falta API. |
| GET /api/savings-account/{accountNumber}/transactions con historial paginado | 20 | **No cumple** | 0 | Endpoint ausente. |
| PATCH /api/savings-account/{accountNumber}/cancel para cancelar secundaria | 20 | **No cumple** | 0 | Endpoint ausente. |
| Transferencia de balance a principal y registro de movimientos al cancelar | 20 | **Parcial** | 10 | Lógica lista en `SavingsAccountAppService`, falta API. |

---

### 15. Módulo API: Gestión de comercios
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| GET /api/commerce con listado paginado de comercios | 20 | **No cumple** | 0 | Módulo de comercios no implementado en API. |
| GET /api/commerce/{id} con detalle del comercio | 20 | **No cumple** | 0 | Endpoint ausente. |
| POST /api/commerce con validación de RNC y correo únicos | 20 | **No cumple** | 0 | Endpoint ausente. |
| PUT /api/commerce/{id} para actualizar datos sin modificar estado | 20 | **No cumple** | 0 | Endpoint ausente. |
| PATCH /api/commerce/{id}/status para activar o desactivar comercio | 20 | **No cumple** | 0 | Endpoint ausente. |
| Inactivación de usuarios asociados al desactivar comercio | 20 | **No cumple** | 0 | Lógica no implementada. |
| Reactivación de comercio sin activar automáticamente sus usuarios | 20 | **No cumple** | 0 | Lógica no implementada. |

---

### 16. Módulo API: Procesador de pago Hermes Pay
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Control de acceso para roles Administrador y Comercio | 20 | **No cumple** | 0 | Módulo Hermes Pay no implementado. |
| Uso del commerceId desde JWT cuando el usuario autenticado es Comercio | 20 | **No cumple** | 0 | Endpoint ausente. |
| Uso del commerceId de la URL cuando el usuario autenticado es Administrador | 20 | **No cumple** | 0 | Endpoint ausente. |
| GET /pay/get-transactions/{commerceId} con transacciones paginadas | 20 | **No cumple** | 0 | Endpoint ausente. |
| POST /pay/process-payment/{commerceId} con validación de tarjeta y comercio | 20 | **No cumple** | 0 | Endpoint ausente. |
| Validación de número de tarjeta, expiración y CVC | 20 | **No cumple** | 0 | Endpoint ausente. |
| Validación de crédito disponible antes de aprobar consumo | 20 | **No cumple** | 0 | Endpoint ausente. |
| Registro de consumo aprobado y aumento de deuda de tarjeta | 20 | **No cumple** | 0 | Endpoint ausente. |
| Acreditación del pago en cuenta principal del comercio | 20 | **No cumple** | 0 | Endpoint ausente. |
| Registro de consumo rechazado sin modificar balances ni deudas | 20 | **No cumple** | 0 | Endpoint ausente. |
| Correos al cliente y al comercio luego de pago aprobado | 20 | **No cumple** | 0 | Endpoint ausente. |

---

### 17. Reglas financieras y trazabilidad
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Uso correcto de tipo DÉBITO para salidas de dinero | 20 | **Cumple** | 20 | Consistente en retiros, transferencias origen y pagos. |
| Uso correcto de tipo CRÉDITO para entradas de dinero | 20 | **Cumple** | 20 | Consistente en depósitos, transferencias destino y desembolsos. |
| Registro cruzado en transferencias entre cuentas | 20 | **Cumple** | 20 | Registra DÉBITO en origen y CRÉDITO en destino. |
| No permitir sobrepagos a tarjetas ni préstamos | 20 | **Cumple** | 20 | Limita pago al monto exacto de la deuda pendiente (`Math.Min`). |
| Actualización correcta de deuda, balance y crédito disponible | 20 | **Cumple** | 20 | Actualizaciones consistentes en todas las entidades. |
| Ejecución transaccional de operaciones que afectan múltiples entidades | 20 | **Cumple** | 20 | `BeginTransactionAsync`, `Commit` y `Rollback` en `UnitOfWork`. |
| Conservación del historial aunque productos o usuarios sean inactivados/cancelados | 20 | **Cumple** | 20 | Cancelación lógica (soft delete) preserva historial completo. |
| Uso de decimal para montos monetarios y precisión hasta centavos | 20 | **Cumple** | 20 | Tipo `decimal` con redondeo a 2 decimales en toda la solución. |

---

### 18. Reglas técnicas y arquitectura
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Implementación en ASP.NET Core MVC y Web API con .NET 9 | 20 | **Cumple** | 20 | TargetFramework `net9.0` en todos los proyectos. |
| Uso correcto de Entity Framework Core Code First | 20 | **Cumple** | 20 | EF Core 9.0 Code First con migraciones. |
| Creación correcta de entidades, relaciones, configuraciones y migraciones | 20 | **Cumple** | 20 | Configuraciones Fluent API en `Infrastructure/Persistence/Configurations`. |
| Implementación correcta de Onion Architecture | 20 | **Cumple** | 20 | Capas Domain, Application, Infrastructure, Persistence, Web, Api. |
| Separación adecuada de capas Domain, Application, Infrastructure, Persistence, WebApp y WebAPI | 20 | **Cumple** | 20 | Referencias unidireccionales correctas. |
| Uso correcto de ViewModels y validaciones en la WebApp | 20 | **Cumple** | 20 | ViewModels organizados por módulo con DataAnnotations. |
| Uso correcto de DTOs para transferencia de información en la API | 20 | **Cumple** | 20 | DTOs organizados en `Application/DTOs`. |
| Uso correcto de AutoMapper entre entidades, ViewModels, DTOs, Commands y Queries | 20 | **Parcial** | 10 | AutoMapper configurado en `AutoMapperProfile.cs`, pero no hay Commands/Queries. |
| Uso de repositorios genéricos y repositorios específicos cuando aplique | 20 | **Cumple** | 20 | `IBaseRepository<T>` y repositorios específicos por módulo. |
| Uso de servicios genéricos y servicios de negocio por módulo | 20 | **Cumple** | 20 | `AppServices` separados por dominio funcional. |
| Controladores sin lógica de negocio compleja | 20 | **Parcial** | 10 | En su mayoría sí, pero `ClientController` y `CashierController` tienen simulación en memoria. |
| Interfaz visual clara usando Bootstrap u otro framework CSS | 20 | **Cumple** | 20 | Bootstrap 5 y estilos CSS personalizados. |

---

### 19. CQRS, Mediator, Behaviors y validaciones
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Implementación de CQRS en endpoints de Account | 20 | **No cumple** | 0 | No se implementó MediatR / CQRS (usa AppServices directos). |
| Implementación de CQRS en endpoints de usuarios y comercios | 20 | **No cumple** | 0 | No implementado. |
| Implementación de CQRS en endpoints de préstamos | 20 | **No cumple** | 0 | No implementado. |
| Implementación de CQRS en endpoints de tarjetas de crédito | 20 | **No cumple** | 0 | No implementado. |
| Implementación de CQRS en endpoints de cuentas de ahorro | 20 | **No cumple** | 0 | No implementado. |
| Implementación de CQRS en endpoints de Hermes Pay | 20 | **No cumple** | 0 | No implementado. |
| Uso correcto de Mediator para Commands y Queries | 20 | **No cumple** | 0 | MediatR no está referenciado ni configurado. |
| Validaciones de Commands y Queries mediante FluentValidation | 20 | **No cumple** | 0 | FluentValidation no está implementado. |
| Uso de Behaviors para validaciones transversales | 20 | **No cumple** | 0 | Pipeline Behaviors no implementados. |
| Separación entre validaciones estructurales y reglas de negocio con acceso a datos | 20 | **Parcial** | 10 | Separación parcial mediante ViewModels/DTOs y servicios, pero sin pipeline de validación. |

---

### 20. Validación de servicios por módulo
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Servicios de seguridad, login, activación y restablecimiento validados correctamente | 20 | **Cumple** | 20 | `AuthAppService` con todas las validaciones requeridas. |
| Servicios de usuarios y roles validados correctamente | 20 | **Cumple** | 20 | `UserAppService` valida unicidad de cédula, correo, usuario. |
| Servicios de cuentas de ahorro y transacciones validados correctamente | 20 | **Cumple** | 20 | `SavingsAccountAppService` valida estado y cuenta principal. |
| Servicios de préstamos, amortización y pagos validados correctamente | 20 | **Cumple** | 20 | `LoanAppService` y `PaymentAppService` con validaciones completas. |
| Servicios de tarjetas, consumos, pagos y avances validados correctamente | 20 | **Cumple** | 20 | `CreditCardAppService` y `CardPaymentAppService` con validaciones completas. |
| Servicios de beneficiarios y transferencias validados correctamente | 20 | **Cumple** | 20 | `BeneficiaryAppService` y `TransferAppService` con validaciones completas. |
| Servicios de cajero validados correctamente | 20 | **Cumple** | 20 | `DepositAppService`, `WithdrawalAppService` y `CardPaymentAppService` validados. |
| Servicios de comercios y Hermes Pay validados correctamente | 20 | **No cumple** | 0 | Servicios de comercios y Hermes Pay ausentes. |
| Servicios de correo desacoplados y reutilizables | 20 | **Cumple** | 20 | `IEmailService` implementado en `Shared` e inyectado. |

---

### 21. Documentación, excepciones y logs
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Documentación Swagger completa para endpoints, parámetros, body y respuestas | 20 | **Parcial** | 10 | Swagger habilitado pero solo cubre Account Login y Loan. |
| Swagger configurado para autenticación JWT | 20 | **Cumple** | 20 | `AddSecurityDefinition("Bearer", ...)` configurado. |
| Global Exception Handler implementado correctamente | 20 | **Parcial** | 10 | Manejador por defecto de MVC `/Home/Error`, falta middleware centralizado en API. |
| Respuestas de error utilizando Problem Details RFC 7807 | 20 | **No cumple** | 0 | Retorna objetos anónimos `{ message = ... }`, no RFC 7807. |
| Manejo centralizado de errores de negocio, validación y no encontrados | 20 | **Parcial** | 10 | Manejo local en controladores/servicios, no centralizado. |
| Serilog configurado en WebApp y WebAPI | 20 | **No cumple** | 0 | Paquete Serilog no instalado; usa `ILogger` estándar. |
| Logs de operaciones financieras relevantes | 20 | **Parcial** | 10 | Existen logs puntuales con `ILogger` en servicios bancarios. |
| Logs de errores no controlados con información útil | 20 | **Parcial** | 10 | Try/catch en controladores y servicios, sin middleware global de logging. |
| No registrar datos sensibles en logs, respuestas, vistas ni correos | 20 | **Cumple** | 20 | Tarjetas enmascaradas, CVC hasheado, passwords protegidas. |

---

### 22. Pruebas unitarias - Commands y Queries
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Unit tests para Commands y Queries de Account | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de usuarios | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de préstamos | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de tarjetas de crédito | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de cuentas de ahorro | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de comercios | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para Commands y Queries de Hermes Pay | 20 | **No cumple** | 0 | No existen pruebas de CQRS. |
| Unit tests para validadores FluentValidation | 20 | **No cumple** | 0 | No existen validadores FluentValidation. |

---

### 23. Pruebas unitarias - Servicios de negocio
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Unit tests de servicios de cuentas y balances | 20 | **No cumple** | 0 | No hay pruebas unitarias para `SavingsAccountAppService`. |
| Unit tests de servicios de transferencias y beneficiarios | 20 | **No cumple** | 0 | No hay pruebas unitarias para `TransferAppService` o `BeneficiaryAppService`. |
| Unit tests de servicios de depósitos y retiros | 20 | **Parcial** | 10 | Pruebas unitarias completas para `WithdrawalAppService` (14 pruebas), faltan depósitos. |
| Unit tests de servicios de pagos a tarjetas | 20 | **Cumple** | 20 | Pruebas completas en `CardPaymentAppServiceTests` (18 pruebas). |
| Unit tests de servicios de pagos a préstamos | 20 | **No cumple** | 0 | No hay pruebas unitarias para `PaymentAppService.PayLoanAsync`. |
| Unit tests de cálculo de cuotas y tabla de amortización | 20 | **No cumple** | 0 | No hay pruebas unitarias para el algoritmo de amortización francés. |
| Unit tests de servicios de tarjetas y avances de efectivo | 20 | **No cumple** | 0 | No hay pruebas unitarias para `CreditCardAppService`. |
| Unit tests de servicios de comercios y procesamiento Hermes Pay | 20 | **No cumple** | 0 | Servicios y pruebas ausentes. |
| Unit tests de reglas de alto riesgo y validaciones financieras críticas | 20 | **No cumple** | 0 | No hay pruebas unitarias para la regla de alto riesgo de préstamos. |

---

### 24. Pruebas de integración - Repositorios y persistencia
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Integration tests de repositorios de usuarios, roles y tokens | 20 | **No cumple** | 0 | No existe proyecto de pruebas de integración. |
| Integration tests de repositorios de cuentas de ahorro | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de persistencia de transacciones financieras | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de repositorios de préstamos y amortización | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de repositorios de tarjetas y consumos | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de repositorios de beneficiarios | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de repositorios de comercios y usuarios de comercio | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Integration tests de operaciones transaccionales con base de datos de prueba | 20 | **No cumple** | 0 | Pruebas de integración ausentes. |
| Uso de InMemory Database o SQLite en memoria sin depender de BD real | 20 | **No cumple** | 0 | Configuración de BD InMemory o SQLite en memoria no implementada. |

---

### 25. Calidad final, entrega y ejecución
| Criterio de evaluación | Valor | Estado | Puntos | Observación |
|---|:---:|:---:|:---:|---|
| Solución compila correctamente sin errores | 20 | **Cumple** | 20 | `dotnet build` compila 100% limpio (0 errores, 0 warnings). |
| Migraciones aplican correctamente y generan la base de datos esperada | 20 | **Cumple** | 20 | Migraciones Code First funcionales (`context.Database.MigrateAsync`). |
| Seed de datos mínimos funcionales para pruebas iniciales | 20 | **Cumple** | 20 | `DefaultRolesAndUsers.SeedAsync` inicializa roles y usuarios default. |
| La WebApp ejecuta correctamente con sus módulos principales | 20 | **Parcial** | 10 | Ejecuta, pero varios módulos web usan listas mock en lugar de la BD real. |
| La WebAPI ejecuta correctamente con Swagger disponible | 20 | **Parcial** | 10 | Ejecuta con Swagger, pero faltan los controladores de Usuarios, Tarjetas, Cuentas, Comercios y Hermes Pay. |
| Las pruebas automatizadas ejecutan correctamente desde la solución | 20 | **Parcial** | 10 | Las 32 pruebas actuales pasan al 100%, pero falta cobertura en el resto de servicios. |
| Manejo adecuado de configuración mediante appsettings y ambientes | 20 | **Cumple** | 20 | Configuración desacoplada con DotNetEnv y variables de entorno. |
| Código organizado, legible, mantenible y consistente | 20 | **Cumple** | 20 | Nomenclatura clara, separación de capas limpia y buenas prácticas de C#. |
| No exposición de datos sensibles en UI, API, correos ni logs | 20 | **Cumple** | 20 | Enmascaramiento de tarjetas y hashing de CVC implementados correctamente. |

---

## 🚀 Plan de Acción para Alcanzar el 100%

1. **Vincular Controladores Web a Servicios de Aplicación:**
   - En [`AdminController.cs`](file:///home/briamco/projects/ArtemisPro/Web/Controllers/AdminController.cs), reemplazar `_dummyUsers` y `_dummyCards` por llamadas directas a `IUserAppService`, `ICreditCardAppService` y `IAdminDashboardAppService`.
   - En [`ClientController.cs`](file:///home/briamco/projects/ArtemisPro/Web/Controllers/ClientController.cs), inyectar los servicios reales (`ISavingsAccountAppService`, `ITransferAppService`, `IBeneficiaryAppService`, `IPaymentAppService`, `ICreditCardAppService`, `ILoanAppService`) para conectar las vistas del cliente con la base de datos.
   - En [`CashierController.cs`](file:///home/briamco/projects/ArtemisPro/Web/Controllers/CashierController.cs), conectar los métodos de depósito (`IDepositAppService`), retiro (`IWithdrawalAppService`), pago a préstamos (`IPaymentAppService`) y transferencias de terceros.

2. **Implementar los Endpoints Faltantes en la Web API:**
   - Crear `UsersController` (listados paginados, creación, actualización, toggle status).
   - Crear `CreditCardController` (listado, asignación, modificación de límite, cancelación).
   - Crear `SavingsAccountController` (listado, cuentas secundarias, transacciones, cancelación).
   - Crear `CommerceController` (CRUD completo de comercios).
   - Crear `PayController` (procesamiento de pagos Hermes Pay y consulta de consumos).
   - Completar `AccountController` en API (`POST /account/confirm`, `POST /account/get-reset-token`, `POST /account/reset-password`).

3. **Arquitectura CQRS / MediatR y FluentValidation:**
   - Si la entrega requiere obligatoriamente CQRS/MediatR, crear Commands y Queries correspondientes con `MediatR` y `FluentValidation` en la capa `Application`.

4. **Completar la Suite de Pruebas Automatizadas:**
   - Agregar pruebas unitarias para `SavingsAccountAppService`, `TransferAppService`, `BeneficiaryAppService`, `DepositAppService`, `LoanAppService` (amortización y alto riesgo) y `CreditCardAppService`.
   - Crear proyecto de Integration Tests (`Tests/Persistence.IntegrationTests`) con base de datos InMemory / SQLite.

5. **Infraestructura de Logs y Manejo Centralizado de Excepciones:**
   - Configurar `Serilog` en Web y API.
   - Implementar middleware de ProblemDetails (RFC 7807) para manejo centralizado de excepciones en Web API.
