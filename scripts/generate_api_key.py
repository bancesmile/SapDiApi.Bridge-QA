#!/usr/bin/env python3
"""
Script generador de API Keys seguras para BridgeSap.
Genera claves criptográficamente seguras con formato estándar y el bloque JSON listo para appsettings.json.
"""

import secrets
import json
import argparse
import sys
from pathlib import Path

# Asegurar compatibilidad de salida UTF-8 en consolas Windows
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass

def generate_key(prefix: str = "sk_live") -> str:
    """Genera una clave criptográficamente segura con entropía de 256 bits."""
    token = secrets.token_urlsafe(32)
    return f"{prefix}_{token}"

def create_client_entry(client_id: str, name: str, api_key: str, companies: list[str]) -> dict:
    return {
        "Id": client_id,
        "Name": name,
        "ApiKey": api_key,
        "IsActive": True,
        "AllowedCompanies": companies
    }

def main():
    parser = argparse.ArgumentParser(
        description="Generador de API Keys seguras para SapDiApi.Bridge",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ejemplos de uso:
  python generate_api_key.py
  python generate_api_key.py --name "Portal Web Facturación" --id "portal-facturacion" --companies "*"
  python generate_api_key.py --name "App Móvil Cobranzas" --id "app-cobranzas" --companies "TEST1_SBO_UNOCINCO,PROD_SBO" --save
        """
    )
    parser.add_argument("--id", help="Identificador único del cliente (ej: portal-web, app-movil)")
    parser.add_argument("--name", help="Nombre descriptivo de la aplicación cliente")
    parser.add_argument("--prefix", default="sk_live", help="Prefijo de la llave (por defecto: sk_live)")
    parser.add_argument("--companies", default="*", help="Sociedades permitidas separadas por comas o '*' para todas (por defecto: *)")
    parser.add_argument("--save", action="store_true", help="Agrega automáticamente el cliente a appsettings.json")

    args = parser.parse_args()

    # Modo interactivo si no se proporcionan argumentos
    client_id = args.id
    name = args.name
    companies_str = args.companies

    if not client_id or not name:
        print("\n🔐 Generador de API Keys para SapDiApi.Bridge")
        print("--------------------------------------------------")
        if not name:
            name = input("📌 Nombre de la aplicación (ej: Portal Web de Autorizaciones): ").strip()
            if not name:
                name = "Nueva Aplicación Web"
        
        if not client_id:
            default_id = name.lower().replace(" ", "-").replace("_", "-")
            input_id = input(f"🏷️  Identificador único [{default_id}]: ").strip()
            client_id = input_id if input_id else default_id
            
        input_companies = input("🏢 Sociedades permitidas separadas por coma [* para todas]: ").strip()
        if input_companies:
            companies_str = input_companies

    companies = [c.strip() for c in companies_str.split(",") if c.strip()]
    if not companies:
        companies = ["*"]

    # Generar la llave
    api_key = generate_key(args.prefix)
    client_entry = create_client_entry(client_id, name, api_key, companies)

    print("\n" + "=" * 60)
    print("✨ API KEY GENERADA EXITOSAMENTE ✨")
    print("=" * 60)
    print(f"🔑 ApiKey:    {api_key}")
    print(f"🏷️  Id:        {client_id}")
    print(f"📌 Nombre:    {name}")
    print(f"🏢 Empresas:  {', '.join(companies)}")
    print("=" * 60)

    json_snippet = json.dumps(client_entry, indent=6)
    print("\n📋 Bloque JSON listo para pegar en 'ApiKeyAuth.Clients' de appsettings.json:\n")
    print(json_snippet)
    print("\n" + "=" * 60)

    # Opción para guardar automáticamente en appsettings.json
    if args.save:
        save_to_appsettings(client_entry)
    else:
        resp = input("\n💾 ¿Deseas agregar este cliente automáticamente a appsettings.json? (s/N): ").strip().lower()
        if resp in ["s", "si", "y", "yes"]:
            save_to_appsettings(client_entry)

def save_to_appsettings(client_entry: dict):
    # Localizar appsettings.json en la raíz del proyecto
    script_dir = Path(__file__).resolve().parent
    root_dir = script_dir.parent
    appsettings_path = root_dir / "SapDiApi.Bridge" / "appsettings.json"

    if not appsettings_path.exists():
        print(f"❌ Error: No se encontró el archivo en {appsettings_path}")
        return

    try:
        with open(appsettings_path, "r", encoding="utf-8") as f:
            data = json.load(f)

        if "ApiKeyAuth" not in data:
            data["ApiKeyAuth"] = {}

        if "Clients" not in data["ApiKeyAuth"]:
            data["ApiKeyAuth"]["Clients"] = []

        # Verificar si ya existe un cliente con ese ID
        existing = next((c for c in data["ApiKeyAuth"]["Clients"] if c.get("Id") == client_entry["Id"]), None)
        if existing:
            print(f"⚠️  Ya existía un cliente con Id '{client_entry['Id']}'. Actualizando...")
            data["ApiKeyAuth"]["Clients"].remove(existing)

        data["ApiKeyAuth"]["Clients"].append(client_entry)

        with open(appsettings_path, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        print(f"✅ ¡Cliente '{client_entry['Name']}' guardado con éxito en appsettings.json!")

    except Exception as e:
        print(f"❌ Error al actualizar appsettings.json: {e}")

if __name__ == "__main__":
    main()
