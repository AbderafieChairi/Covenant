import requests
import warnings
warnings.filterwarnings("ignore")
import json
import jwt

url = "https://127.0.0.1:7443/api/users/login"

headers = {
    "accept": "text/plain",
    "Content-Type": "application/json-patch+json"
}

data = {
    "id": "string",
    "userName": "user",
    "password": "user"
}

response = requests.post(
    url,
    headers=headers,
    data=json.dumps(data),
    verify=False
)

print(f"Login Status: {response.status_code}")
success = json.loads(response.text)['success']
print(f"Success: {success}")
token = json.loads(response.text)['covenantToken']
print("-" * 50)

# Decode the JWT to see what claims are actually in it
decoded = jwt.decode(token, options={"verify_signature": False})
print("JWT Claims:")
for key, value in decoded.items():
    print(f"  {key}: {value}")
print("-" * 50)

# Now test the /api/users/current endpoint
url = "https://127.0.0.1:7443/api/users/current"
print(f"Testing: {url}")
headers = {
    "accept": "text/plain",
    "Authorization": 'Bearer ' + token
}

response = requests.get(url, headers=headers, verify=False)
print(f"Status: {response.status_code}")

# Write full response to file
with open("claims_response.txt", "w", encoding="utf-8") as f:
    f.write(f"Status: {response.status_code}\n")
    f.write(f"Full Response:\n{response.text}\n")

print(f"Response written to claims_response.txt")
print(f"First 500 chars: {response.text[:500]}")
