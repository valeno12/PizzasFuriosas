# Guía de deploy — Pizzas Furiosas

Todo con planes **gratis**. Se hostean 3 cosas:

| Pieza | Qué es | Dónde | URL que te queda |
|---|---|---|---|
| `backend` | API .NET 8 + Postgres | **Render** (Docker) + **Neon** (base) | `pizzafuriosa-api.onrender.com` |
| `front-landing` | Landing pública (Astro) | **Cloudflare Pages** | `pizzafuriosa.pages.dev` |
| `admin-front` | Panel de administración | **Cloudflare Pages** (otro proyecto) | `pizzafuriosa-admin.pages.dev` |

> **El dominio propio va al final.** Primero se publica todo con las URLs gratis, se
> prueba, y recién ahí (cuando quieras) se compra el dominio y se apunta. El dominio
> es lo único que cuesta plata (~USD 10-15/año).

---

## Paso 0 — Subir el repo a GitHub

Render y Cloudflare publican **desde un repo de GitHub** (cada vez que hacés `push`,
se redeploya solo). Si todavía no está, creá un repo privado y subí el proyecto.

## Paso 1 — Base de datos (Neon)

1. Crear cuenta en **neon.tech** (gratis).
2. Crear un proyecto → nombre `pizzasfuriosas`, región la más cercana (ej. AWS São Paulo).
3. Copiar el **connection string** que te muestra. Se ve así:
   ```
   postgresql://usuario:clave@ep-xxxx.sa-east-1.aws.neon.tech/pizzasfuriosas?sslmode=require
   ```
   Ese valor es el `DATABASE_URL`. Guardalo, se usa en el Paso 3.

> No hay que crear tablas a mano: la API aplica las migraciones sola al arrancar.

## Paso 2 — Cloudinary (fotos de productos)

Ya lo usan. Entrá a **cloudinary.com** → Dashboard y anotá:
`CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`.
(Si no tienen cuenta, se crea gratis en 2 minutos.)

## Paso 3 — Backend (Render)

1. Crear cuenta en **render.com** (entrar con GitHub).
2. **New +** → **Web Service** → elegir el repo.
3. Configuración:
   - **Root Directory:** `backend`
   - **Runtime / Language:** Docker (lo detecta por el `Dockerfile`).
   - **Instance Type:** Free.
   - **Health Check Path:** `/`
4. En **Environment**, agregar estas variables:

   | Variable | Valor |
   |---|---|
   | `DATABASE_URL` | el de Neon (Paso 1) |
   | `JWT_KEY` | una clave larga y random (32+ caracteres) |
   | `JWT_ISSUER` | `PizzasFuriosas.Api` |
   | `JWT_AUDIENCE` | `PizzasFuriosas.Client` |
   | `JWT_EXPIRATION_HOURS` | `720` |
   | `CLOUDINARY_CLOUD_NAME` | (Paso 2) |
   | `CLOUDINARY_API_KEY` | (Paso 2) |
   | `CLOUDINARY_API_SECRET` | (Paso 2) |
   | `ADMIN_DEFAULT_EMAIL` | el mail del admin (ej. `pizza@furiosa.com`) |
   | `ADMIN_DEFAULT_PASSWORD` | una contraseña segura (con esta se loguea al panel) |

   > **No** definas `PORT`: Render lo asigna solo y la API ya lo detecta.

5. **Create Web Service.** El primer build tarda unos minutos. Cuando termine, probá
   en el navegador `https://TU-API.onrender.com/` → tiene que responder
   `{"status":"ok",...}`. La carta está en `.../api/categories`.

## Paso 4 — Landing (Cloudflare Pages)

1. Crear cuenta en **dash.cloudflare.com** → **Workers & Pages** → **Create** →
   **Pages** → **Connect to Git** → elegir el repo.
2. Configuración de build:
   - **Root directory (advanced):** `front-landing`
   - **Framework preset:** Astro
   - **Build command:** `npm run build`
   - **Build output directory:** `dist`
3. En **Environment variables** (Production):

   | Variable | Valor |
   |---|---|
   | `PUBLIC_API_URL` | `https://TU-API.onrender.com/api` |
   | `PUBLIC_WHATSAPP_PHONE` | `5493434537186` (solo dígitos, formato internacional) |
   | `PUBLIC_PHONE_DISPLAY` | `+54 9 3434 53-7186` |
   | `NODE_VERSION` | `22` |

   > Ojo: `PUBLIC_API_URL` termina en **`/api`**.

4. **Save and Deploy.** Te queda `https://pizzafuriosa.pages.dev`.

## Paso 5 — Probar de punta a punta

Abrí la landing publicada → la sección **Carta** debe cargar los productos desde la
API. Agregá algo al pedido y probá el botón de WhatsApp.

> **Primera carga lenta:** en el plan free de Render la API "se duerme" tras un rato
> sin uso; la primera visita puede tardar ~30-60s en despertar. Si molesta, se
> configura un ping automático (ej. cron-job.org cada 10 min a la URL de la API).

## Paso 6 — Admin (opcional, mismo método que la landing)

Repetir el Paso 4 con **Root directory:** `admin-front` y la variable que use para
apuntar a la API. Se entra con el `ADMIN_DEFAULT_EMAIL` / `ADMIN_DEFAULT_PASSWORD`
del Paso 3.

## Paso 7 — Dominio propio (cuando quieras)

1. Comprar el dominio (ej. Cloudflare Registrar, o NIC Argentina para `.com.ar`).
2. En Cloudflare Pages → tu proyecto → **Custom domains** → agregar el dominio.
3. Para la API, opcional: subdominio `api.tudominio.com` apuntando a Render.

---

## Notas / pendientes

- **CORS:** hoy la API acepta cualquier origen (`AllowAnyOrigin`, ver `Program.cs`).
  Funciona, pero antes de usar dominio propio conviene restringirlo a la URL de la
  landing.
- **Seguridad:** el admin por defecto se crea con `ADMIN_DEFAULT_PASSWORD` — usá una
  contraseña fuerte y no la compartas en el repo.
- **`node_modules`/`dist`** no deberían estar en el repo; si están commiteados, sumalos
  al `.gitignore` y borralos del índice (`git rm -r --cached ...`).
