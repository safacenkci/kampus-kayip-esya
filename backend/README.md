# Kampüs Kayıp-Eşya API

ASP.NET Core 8 Web API for the Aksaray University campus lost-and-found board.
Listens on `http://localhost:5080`. CORS allows `http://localhost:4200`.

Fixed catalogs:

- Locations: `merkez`, `kütüphane`, `yemekhane`, `mühendislik`, `yurt`, `spor salonu`
- Categories: `öğrenci kartı`, `anahtar`, `telefon`, `çanta`, `kıyafet`, `kulaklık`, `diğer`

`contact` is omitted while `status` is `open`. `GET /api/items/{id}` includes `statusHistory`.

## Run locally

1. Start PostgreSQL:

```bash
docker compose up -d postgres
```

2. Run the API (applies migrations and seeds sample items on first start):

```bash
cd backend
dotnet run
```

The API is then available at `http://localhost:5080`. Connection string in `appsettings.json` matches the compose service (`postgres` / `postgres` / `kampus_kayip_esya` on port `5432`).
