# 🍕 Pizzas Furiosas

Plataforma web para una pizzería real de Paraná (Entre Ríos): una **landing** donde el cliente arma su pedido y lo cierra por WhatsApp, un **panel de administración** para gestionar el negocio, y una **API** que sostiene todo.

Proyecto full-stack de punta a punta — desde el modelado de datos y la API hasta el deploy en la nube — construido y desplegado para uso real.

> 🔗 **Demo:** _landing en producción_ · **Estado:** en producción y en mejora continua.

---

## ✨ Qué hace

**Landing (cliente)**
- Carta en vivo servida desde la API (categorías + productos con precios).
- Armado de pedido con carrito persistente y cierre por WhatsApp (sin checkout automático).
- Responsive, con foco en mobile.

**Panel admin**
- Login con JWT.
- ABM de productos y categorías (con subida de imágenes a Cloudinary).
- Gestión de pedidos, clientes, eventos y control de gastos/compras.
- Dashboard con métricas del negocio.

---

## 🛠️ Stack

| Capa | Tecnologías |
|---|---|
| **Backend** | .NET 8 (ASP.NET Core), EF Core, PostgreSQL, JWT, FluentValidation, Cloudinary, Docker |
| **Admin** | Vue 3, Vite, Pinia, Vue Router, Tailwind CSS, Axios |
| **Landing** | Astro (sitio estático) |
| **Infra** | Render (API en Docker) · Neon (Postgres) · Cloudflare Pages (fronts) |

## 🧱 Arquitectura del backend

Separado en capas para mantener el dominio independiente de la infraestructura:

```
backend/src/
├── PizzasFuriosas.Core            → entidades, interfaces, validadores (dominio)
├── PizzasFuriosas.Infrastructure  → EF Core, migraciones, servicios (Cloudinary, etc.)
└── PizzasFuriosas.Api             → controllers, configuración, endpoints
```

- Migraciones que se aplican solas al arrancar (ideal para hosting sin consola).
- Configuración por variables de entorno (nada de secretos en el repo).
- Respuestas de API uniformes y validación con mensajes en español.

## 📂 Estructura del repo

```
├── backend/         → API .NET
├── admin-front/     → panel de administración (Vue)
├── front-landing/   → landing pública (Astro)
└── docker-compose.yml → Postgres para desarrollo local
```

---

## 🚀 Correr en local

**Requisitos:** .NET 8 SDK, Node 20+, Docker.

**1. Base de datos**
```bash
docker compose up -d        # levanta Postgres local
```

**2. Backend** — crear `backend/.env` (ver `backend/.env.example`) y:
```bash
cd backend/src/PizzasFuriosas.Api
dotnet run                  # API en http://localhost:5054
```

**3. Landing**
```bash
cd front-landing
npm install && npm run dev
```

**4. Admin**
```bash
cd admin-front
npm install && npm run dev
```

---

## 🗺️ Roadmap

Proyecto vivo, en mejora continua. Próximos focos:
- Reforzar buenas prácticas y arquitectura del backend.
- Tests automatizados.
- Refinamientos de UX en el panel.

---

_Desarrollado por [valeno12](https://github.com/valeno12)._
