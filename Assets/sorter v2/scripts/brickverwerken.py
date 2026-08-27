import sys
import os
import requests
import json

def herken_afbeelding(image_path, url):
    """Verstuurt één specifieke afbeelding naar Brickognize."""
    headers = {"accept": "application/json"}
    try:
        with open(image_path, 'rb') as f:
            # Belangrijk: Brickognize verwacht de sleutel 'query_image'
            files = {'query_image': (os.path.basename(image_path), f, 'image/png')}
            
            response = requests.post(url, headers=headers, files=files)
            
            if response.status_code == 200:
                return response.json()
            else:
                print(f"Error [{os.path.basename(image_path)}]: Statuscode {response.status_code}")
                return None
    except Exception as e:
        print(f"Error tijdens request voor {image_path}: {str(e)}")
        return None

def herken_meerdere_afbeeldingen(image_paths):
    # 1. Controleer welke bestanden bestaan
    valid_paths = [path for path in image_paths if os.path.exists(path)]
    
    if not valid_paths:
        print("Error: Geen enkele geldige afbeelding gevonden om te verwerken.")
        return

    # Correcte API URL van Brickognize
    url_publiek = "https://api.brickognize.com/predict/"
    
    best_results = []

    # 2. Loop door elke foto en doe een aparte request
    for path in valid_paths:
        print(f"Verwerken van: {os.path.basename(path)}...")
        data = herken_afbeelding(path, url_publiek)
        
        if data and 'items' in data and len(data['items']) > 0:
            # Pak de beste match van deze specifieke foto
            best_match = data['items'][0]
            best_results.append({
                'file': os.path.basename(path),
                'id': best_match.get('id', 'unknown'),
                'name': best_match.get('name', 'Onbekend Onderdeel'),
                'confidence': best_match.get('confidence', 0)
            })

    # 3. Evalueer de resultaten van beide camera's
    if best_results:
        # Sorteer de resultaten op de hoogste confidence score
        best_results.sort(key=lambda x: x['confidence'], reverse=True)
        top_match = best_results[0]
        
        # Dit vangt je Unity-script op via p.StandardOutput
        print(f"Detected LEGO part: {top_match['id']} - {top_match['name']} (Score: {top_match['confidence']} via {top_match['file']})")
    else:
        print("Detected LEGO part: unknown - Geen match gevonden op beide camera's")

if __name__ == "__main__":
    if len(sys.argv) > 1:
        image_arguments = sys.argv[1:]
        herken_meerdere_afbeeldingen(image_arguments)
    else:
        print("Error: Geen afbeeldingen meegegeven aan het Python script.")