# Guía Paso a Paso para Publicar MultiScreenDock en la Microsoft Store (Partner Center)

## 1. Identidad Oficial Configurada en el Paquete MSIX (`Package.appxmanifest`)

Ya están integrados tus 3 identificadores oficiales de Microsoft Partner Center (`ffgomez@outlook.com`):
- **`Package/Identity/Name`:** `FernandoGomezSancha.MultiScreenDock`
- **`Package/Identity/Publisher`:** `CN=472BFB1F-F75A-4F46-9A1F-11627E3DED87`
- **`Package/Properties/PublisherDisplayName`:** `Fernando Gomez Sancha`

---

## 2. ¿Por qué no te pidió datos bancarios al darte de alta y dónde se configuran?

Microsoft separa el **alta como desarrollador** (que te permite publicar apps gratuitas desde el minuto 1) del **perfil de cobros e impuestos** (necesario únicamente cuando vas a ponerle precio a una app o vender compras integradas como *SinteMed Pro*):

### Cómo activar los cobros (IBAN + perfil fiscal) en Partner Center:
1. En Partner Center ([partner.microsoft.com/dashboard/home](https://partner.microsoft.com/dashboard/home)), haz clic arriba a la derecha en el icono de **rueda dentada ⚙️ (`Account settings` / `Configuración de la cuenta`)**.
2. En el menú lateral izquierdo, baja al apartado **`Payout and tax` (`Pagos e impuestos`)**:
   - Haz clic en **`Payout and tax profile assignment` (`Asignación de perfiles de pago e impuestos`)** o **`Payment and tax profiles`**.
   - Añade tu **cuenta bancaria (IBAN español)** para recibir las transferencias en euros (`EUR`) y completa el formulario fiscal electrónico (W-8BEN para residentes en España, con el convenio de doble imposición España-EE.UU. al 0% de retención).

*(Truco práctico: puedes enviar hoy mismo `MultiScreenDock` a revisión en la Microsoft Store —ya sea gratuita o con el precio que elijas en cuanto actives el perfil de pagos—).*

---

## 3. Cómo Completar el Envío (`Start your submission`) en Partner Center

Dentro de la página de **`MultiScreenDock`** en Partner Center (`Application overview`), haz clic en **`Start your submission` (`Iniciar el envío`)** y completa las 6 filas en orden:

### Fila 1 — `Pricing and availability` (`Precios y disponibilidad`)
- **Markets (Mercados):** Deja todos seleccionados (241 mercados).
- **Visibility:** `Make this product available in the Store` (Público).
- **Base price (Precio base):**
  - Elige **`4.99 USD / 4,99 €`** (si ya has completado el perfil de pagos en `⚙️ → Payout and tax`) o **`Free` (`Gratis`)** si prefieres publicarla ya mismo sin esperar a la validación bancaria y cambiar el precio después.
- Pulsa **`Save draft` (`Guardar borrador`)**.

---

### Fila 2 — `Properties` (`Propiedades`)
- **Category (Categoría):** **`Utilities & tools` (`Utilidades y herramientas`)** -> Subcategoría: **`Desktop`** (o **`Productivity`**).
- **Privacy policy URL (URL de la política de privacidad):**
  `https://multiscreendock.fgomsan.here.now/privacy.html`
- **Website:**
  `https://multiscreendock.fgomsan.here.now/`
- **Support contact info (Correo de soporte):**
  `ffgomez@outlook.com` (o `fgs@icua.es`)
- **Display mode:** Marca ✅ **`PC / Desktop`**.
- **System requirements:** Deja los valores por defecto.
- Pulsa **`Save` (`Guardar`)**.

---

### Fila 3 — `Age ratings` (`Clasificaciones por edades`)
- **Category:** Elige **`Utility, Productivity, Communication, or Other`**.
- Marca **`No`** a todas las preguntas de violencia, contenido adulto, compras aleatorias (loot boxes), etc.
- Pulsa **`Save and generate`** (obtendrás **PEGI 3 / ESRB Everyone** en el acto) y luego **`Continue`**.

---

### Fila 4 — `Packages` (`Paquetes`)
Arrastra a la caja de subida los **2 paquetes oficiales `.msix`** que te he dejado descargados en [`/Users/fernando/Desktop/antigravity/MultiScreenDock-Windows/releases/`](file:///Users/fernando/Desktop/antigravity/MultiScreenDock-Windows/releases/):
1. 📦 **`MultiScreenDock-v1.5.0-x64.msix`**
2. 📦 **`MultiScreenDock-v1.5.0-arm64.msix`**

Marca ✅ **`Windows 10/11 Desktop`** y pulsa **`Save`**.

---

### Fila 5 — `Store listings` (`Descripciones de Store`)
Haz clic en **`English`** (y si añades **`Spanish`**, rellena también la española):

#### 🇬🇧 English Store Listing
- **Description:**
  ```text
  MultiScreenDock brings a fast, native macOS-style frosted-glass Dock simultaneously to every monitor on Windows 10 and Windows 11 — restoring Left, Right, and Bottom Dock positions, 120 Hz raised-cosine magnification with focal lock, Smart Auto-Hide (IntelliHide), and interactive Folder Stacks across 2 to 6+ displays.

  KEY FEATURES
  • Simultaneous Dock on Every Monitor: Launch, switch, or restore apps with a single click on the exact display you are looking at — including stacked upper and lower monitor arrays.
  • Bottom, Left & Right Edge Positions: Pin your Dock to the Bottom, Left, or Right edge of every monitor (restoring vertical screen-edge bars on Windows 11) with full vertical 120 Hz cosine magnification.
  • 3 Intelligent Visibility Modes:
    1. Smart Auto-Hide (IntelliHide): Stays visible when a monitor's Dock zone is clear, and hides per-monitor only when a window overlaps the Dock area.
    2. Edge Auto-Hide: Reveals smoothly on hover with an anti-crossing dwell filter for stacked and side-by-side monitors.
    3. Always Visible: Keeps the Dock permanently visible across your screens.
  • Interactive Folder Stacks (Grid, Fan & List): Click Downloads or Documents on any monitor to open an interactive Folder Stack with subfolder navigation and direct file drag-and-drop into any application.
  • 120 Hz Cosine Magnification Wave with Focal Lock: Smooth distance-based icon wave that keeps the icon under your pointer anchored against sideways drift while clicking.
  • Light Glass & Dark Glass Appearance: Automatically follows your Windows 10/11 Light or Dark theme or locks to Light Glass or Dark Glass.
  • Per-Monitor Fullscreen Auto-Suppression: Automatically steps out of the way on any display running a fullscreen video, presentation, or game while remaining active on your other monitors.
  • 8 Built-In Languages & 100% Private: English, Español, Français, Deutsch, Português, Italiano, 日本語, and 简体中文 — with zero telemetry and zero cloud dependencies.
  ```
- **Short description:**
  `Simultaneous macOS-style multi-monitor Dock for Windows 10 & 11 with Bottom, Left & Right positions, 120 Hz wave magnification, Smart Auto-Hide, and Folder Stacks.`
- **Screenshots (Capturas de pantalla — apartado Desktop):**
  Arrastra las 5 capturas desde [`/Users/fernando/Desktop/antigravity/MultiScreenDock-Promo/mas/screenshots-en-2560x1600/`](file:///Users/fernando/Desktop/antigravity/MultiScreenDock-Promo/mas/screenshots-en-2560x1600/) (y para español desde [`screenshots-es-2560x1600/`](file:///Users/fernando/Desktop/antigravity/MultiScreenDock-Promo/mas/screenshots-es-2560x1600/)).
- **Store logos (300×300 px):**
  Sube [`/Users/fernando/Desktop/antigravity/MultiScreenDock-Windows/packaging/msix/Assets/StoreIcon300x300.png`](file:///Users/fernando/Desktop/antigravity/MultiScreenDock-Windows/packaging/msix/Assets/StoreIcon300x300.png).
- **Search terms (hasta 7 términos):**
  `dock`, `multi-monitor`, `taskbar`, `desktop`, `productivity`, `launcher`, `multitasking`

---

### Fila 6 — `Submission options` (`Opciones de envío`)
- **Notes for certification (Notas para la certificación):**
  ```text
  MultiScreenDock is a native desktop dock utility for Windows 10 and Windows 11.
  - Upon launch, it immediately renders a translucent floating Dock bar on every connected display (Default: Bottom edge, Smart Auto-Hide mode) and adds a "MultiScreenDock" icon to the Windows System Tray.
  - Right-clicking the Dock bar or the System Tray icon opens the configuration menu (Visibility Mode, Position: Bottom / Left / Right, Appearance: System / Light Glass / Dark Glass, Icon Size, Folder Stack View: Grid / Fan / List, and Language).
  - Note on runFullTrust capability: MultiScreenDock is a packaged desktop WPF application (Windows.FullTrustApplication) that reads the user's pinned taskbar shortcuts and enumerates desktop monitor/window bounds to render a multi-monitor application launcher.
  ```
- Pulsa **`Save`** y finalmente **`Submit to the Store` (`Enviar a la tienda`)**.
