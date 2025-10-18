import json
import os

def migrate_inventory(file_path="user_inventory.json", output_file_path="user_inventory_migrated.json"):
    if not os.path.exists(file_path):
        print(f"Error: El archivo '{file_path}' no se encontró.")
        return

    with open(file_path, 'r', encoding='utf-8-sig') as f:
        inventory_data = json.load(f)

    new_inventory = {}
    old_inventory_entries = {}

    # Separar entradas nuevas (ID numérico) de las antiguas (nombre de usuario)
    for key, data in inventory_data.items():
        if key.isdigit():
            new_inventory[key] = data
        else:
            old_inventory_entries[key] = data

    # Crear un mapeo de userName a ID numérico para búsquedas rápidas
    # Esto mapea el 'userName' dentro de las entradas numéricas a su ID numérico, en minúsculas
    user_name_to_id = {data["userName"].lower(): key for key, data in new_inventory.items() if data.get("userName")}

    # Fusionar datos de inventarios antiguos en los nuevos
    for old_user_name, old_data in old_inventory_entries.items():
        target_id = None
        
        # Convertir a minúsculas para la comparación
        old_user_name_lower = old_user_name.lower()
        
        # Obtener el userName de old_data, si existe y no es None, convertir a minúsculas
        old_data_user_name = old_data.get("userName")
        old_data_user_name_lower = old_data_user_name.lower() if old_data_user_name else ""

        # Caso 1: La clave antigua (nombre de usuario en minúsculas) coincide con un userName en el mapeo
        if old_user_name_lower in user_name_to_id:
            target_id = user_name_to_id[old_user_name_lower]
        # Caso 2: El 'userName' dentro de la entrada antigua (en minúsculas) coincide con un userName en el mapeo
        elif old_data_user_name_lower and old_data_user_name_lower in user_name_to_id:
            target_id = user_name_to_id[old_data_user_name_lower]
        
        if target_id:
            # Fusionar inventario de personajes
            for star_type in ["4_star", "5_star", "6_star"]:
                if star_type in old_data and old_data[star_type]:
                    if star_type not in new_inventory[target_id]:
                        new_inventory[target_id][star_type] = []
                    new_inventory[target_id][star_type].extend(old_data[star_type])
                    # Eliminar duplicados manteniendo el orden original (o casi)
                    new_inventory[target_id][star_type] = list(dict.fromkeys(new_inventory[target_id][star_type]))

            # Sumar contadores
            new_inventory[target_id]["total_pulls"] = new_inventory[target_id].get("total_pulls", 0) + old_data.get("total_pulls", 0)
            new_inventory[target_id]["keys"] = new_inventory[target_id].get("keys", 0) + old_data.get("keys", 0)
            new_inventory[target_id]["pulls_until_guaranteed_5_star"] = min(new_inventory[target_id].get("pulls_until_guaranteed_5_star", 90), old_data.get("pulls_until_guaranteed_5_star", 90)) # Mantener el menor para la garantía

            print(f"Inventario de '{old_user_name}' (antiguo) fusionado con ID '{target_id}' (nuevo).")
        else:
            # Si no hay una entrada numérica para este usuario antiguo, lo mantenemos tal cual
            print(f"Advertencia: No se encontró una entrada numérica para el usuario antiguo '{old_user_name}'. Se mantendrá como está.")
            new_inventory[old_user_name] = old_data


    # Guardar el inventario migrado en un nuevo archivo
    with open(output_file_path, 'w', encoding='utf-8') as f:
        json.dump(new_inventory, f, indent=2, ensure_ascii=False)

    print(f"Migración completada. El inventario actualizado se guardó en '{output_file_path}'.")

if __name__ == "__main__":
    migrate_inventory()
