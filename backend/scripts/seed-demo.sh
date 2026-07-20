#!/usr/bin/env bash
#
# Carga datos de demo por la API (para probar la app y sacar capturas del portfolio).
# Requiere: la API corriendo, Postgres levantado, curl y python3.
#
# Uso:
#   ./scripts/seed-demo.sh
#   API_URL=http://localhost:5054 ./scripts/seed-demo.sh
#
# Las credenciales del admin se leen del backend/.env (ADMIN_DEFAULT_EMAIL / PASSWORD),
# o podés pasarlas por env: ADMIN_EMAIL=... ADMIN_PASS=... ./scripts/seed-demo.sh

API_URL="${API_URL:-http://localhost:5054}"
ENV_FILE="$(dirname "$0")/../.env"

ADMIN_EMAIL="${ADMIN_EMAIL:-$(grep -E '^ADMIN_DEFAULT_EMAIL=' "$ENV_FILE" 2>/dev/null | cut -d= -f2- | tr -d '"'\''\r')}"
ADMIN_PASS="${ADMIN_PASS:-$(grep -E '^ADMIN_DEFAULT_PASSWORD=' "$ENV_FILE" 2>/dev/null | cut -d= -f2- | tr -d '"'\''\r')}"

# Lee un campo de un JSON que llega por stdin. Ej: extract "['data']['id']"
extract() {
  python3 -c "import sys,json
try:
    print(json.load(sys.stdin)$1)
except Exception:
    pass"
}

post() {  # post PATH JSON  -> imprime la respuesta
  curl -s -X POST "$API_URL$1" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "$2"
}

echo "→ Login como $ADMIN_EMAIL en $API_URL"
LOGIN=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}")
TOKEN=$(echo "$LOGIN" | extract "['data']['token']")

if [ -z "$TOKEN" ]; then
  echo "✗ No se pudo obtener el token. ¿La API está corriendo en $API_URL y las credenciales son correctas?"
  echo "  Respuesta: $LOGIN"
  exit 1
fi
echo "✓ Token obtenido"

# --- Productos (categorías seed: 1=Pizzas, 2=Papas, 3=Postres) ---
echo "→ Creando productos"
P_MUZZA=$(post /api/products '{"name":"Muzzarella","price":8500,"categoryId":1,"isAvailable":true}' | extract "['data']['id']")
P_NAPO=$(post /api/products '{"name":"Napolitana","price":9800,"categoryId":1,"isAvailable":true}' | extract "['data']['id']")
P_FUGA=$(post /api/products '{"name":"Fugazzeta","price":9200,"categoryId":1,"isAvailable":true}' | extract "['data']['id']")
P_CALA=$(post /api/products '{"name":"Calabresa","price":10500,"categoryId":1,"isAvailable":true}' | extract "['data']['id']")
P_ESPE=$(post /api/products '{"name":"Especial de la casa","price":12000,"categoryId":1,"isAvailable":false}' | extract "['data']['id']")
P_PAPAS=$(post /api/products '{"name":"Papas fritas","price":4500,"categoryId":2,"isAvailable":true}' | extract "['data']['id']")
P_CHEDDAR=$(post /api/products '{"name":"Papas con cheddar","price":6200,"categoryId":2,"isAvailable":true}' | extract "['data']['id']")
P_FLAN=$(post /api/products '{"name":"Flan casero","price":3500,"categoryId":3,"isAvailable":true}' | extract "['data']['id']")
P_HELADO=$(post /api/products '{"name":"Helado 1/4","price":3000,"categoryId":3,"isAvailable":true}' | extract "['data']['id']")

# --- Clientes ---
echo "→ Creando clientes"
C_JUAN=$(post /api/customers '{"name":"Juan Pérez","phone":"3512345678","notes":"Cliente frecuente"}' | extract "['data']['id']")
C_MARIA=$(post /api/customers '{"name":"María Gómez","phone":"3517654321","notes":null}' | extract "['data']['id']")
C_PEDRO=$(post /api/customers '{"name":"Pedro López","phone":"3513334455","notes":"Sin cebolla siempre"}' | extract "['data']['id']")
C_LUCIA=$(post /api/customers '{"name":"Lucía Fernández","phone":"3519988776","notes":null}' | extract "['data']['id']")

# --- Pedidos ---
echo "→ Creando pedidos"
mk_order() { post /api/orders "$1" | extract "['data']"; }
set_status() { post "/api/orders/$1/status" "{\"statusId\":$2}" >/dev/null; }

O1=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Take Away","deliveryCost":0,"paymentMethod":"Efectivo","items":[{"productId":%s,"quantity":2},{"productId":%s,"quantity":1}]}' "$C_JUAN" "$P_MUZZA" "$P_PAPAS")")
O2=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Delivery","deliveryCost":1500,"paymentMethod":"Transferencia","newAddress":{"street":"Av. Colón","number":"1234","apartment":"3B","notes":"Tocar timbre"},"items":[{"productId":%s,"quantity":1},{"productId":%s,"quantity":1}]}' "$C_MARIA" "$P_NAPO" "$P_FLAN")")
O3=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Delivery","deliveryCost":1800,"paymentMethod":"Efectivo","newAddress":{"street":"San Martín","number":"456","apartment":null,"notes":null},"items":[{"productId":%s,"quantity":3}]}' "$C_PEDRO" "$P_CALA")")
O4=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Take Away","deliveryCost":0,"paymentMethod":"Transferencia","items":[{"productId":%s,"quantity":2},{"productId":%s,"quantity":2}]}' "$C_LUCIA" "$P_FUGA" "$P_CHEDDAR")")
O5=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Delivery","deliveryCost":1500,"paymentMethod":"Efectivo","newAddress":{"street":"Belgrano","number":"789","apartment":null,"notes":"Casa verde"},"items":[{"productId":%s,"quantity":1},{"productId":%s,"quantity":1}]}' "$C_JUAN" "$P_MUZZA" "$P_HELADO")")
O6=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Take Away","deliveryCost":0,"paymentMethod":"Efectivo","items":[{"productId":%s,"quantity":1}]}' "$C_MARIA" "$P_NAPO")")
O7=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Delivery","deliveryCost":2000,"paymentMethod":"Transferencia","newAddress":{"street":"Rivadavia","number":"321","apartment":"1A","notes":null},"items":[{"productId":%s,"quantity":4}]}' "$C_PEDRO" "$P_MUZZA")")
O8=$(mk_order "$(printf '{"customerId":%s,"shippingMethod":"Take Away","deliveryCost":0,"paymentMethod":"Efectivo","items":[{"productId":%s,"quantity":2},{"productId":%s,"quantity":1}]}' "$C_LUCIA" "$P_CALA" "$P_FLAN")")

# Estados variados (5=Entregado da ingresos al dashboard, 6=Cancelado, 1/2/3=en curso)
set_status "$O1" 5
set_status "$O2" 5
set_status "$O3" 5
set_status "$O4" 5
set_status "$O5" 5
set_status "$O6" 2
set_status "$O7" 1
set_status "$O8" 6

# --- Eventos ---
echo "→ Creando eventos"
post /api/events "$(printf '{"customerId":%s,"eventDate":"2026-08-20T21:00:00Z","location":"Salón Las Palmas","notes":"Cumpleaños de 40","pizzaCount":30,"pricePerPizza":7000,"deposit":50000,"surcharges":[{"description":"Mozo extra","amount":15000}],"payments":[{"paymentMethod":"Transferencia","amount":50000}]}' "$C_JUAN")" >/dev/null
post /api/events "$(printf '{"customerId":%s,"eventDate":"2026-06-10T20:00:00Z","location":"Quincho del club","notes":null,"pizzaCount":20,"pricePerPizza":6500,"deposit":30000,"surcharges":[],"payments":[{"paymentMethod":"Efectivo","amount":30000}]}' "$C_MARIA")" >/dev/null

# --- Gastos ---
echo "→ Creando gastos"
post /api/purchases '{"date":"2026-07-05T10:00:00Z","description":"Harina x 25kg","amount":18000,"category":"Insumos"}' >/dev/null
post /api/purchases '{"date":"2026-07-08T10:00:00Z","description":"Muzzarella x 10kg","amount":45000,"category":"Insumos"}' >/dev/null
post /api/purchases '{"date":"2026-07-12T10:00:00Z","description":"Cajas de pizza x 500","amount":22000,"category":"Packaging"}' >/dev/null
post /api/purchases '{"date":"2026-07-15T10:00:00Z","description":"Factura de gas","amount":12500,"category":"Servicios"}' >/dev/null

echo ""
echo "✓ Listo. Datos de demo cargados:"
echo "  · 9 productos, 4 clientes, 8 pedidos (5 entregados, 1 en preparación, 1 pendiente, 1 cancelado)"
echo "  · 2 eventos, 4 gastos"
echo "  Entrá al admin-front con el usuario admin y revisá el dashboard."
