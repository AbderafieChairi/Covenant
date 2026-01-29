import requests
import warnings
warnings.filterwarnings("ignore")
import json
url = "https://127.0.0.1:7443/api/users/login"

headers = {
    "accept": "text/plain",
    "Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyIiwianRpIjoiODE5NGRhNzQtNjZiZi05MjBhLWY5MjgtNzZkMjE1ODVlMGQzIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiJmZDM4ZWNmNi1hYmE1LTQ4ODgtOGVkMi1lNmRhNWQ5YmQxNGQiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiVXNlciIsIkFkbWluaXN0cmF0b3IiXSwiZXhwIjoxNzc4MjcwMjQ2LCJpc3MiOiJDb3ZlbmFudCIsImF1ZCI6IkNvdmVuYW50In0.S37WYBGYU2ll9Xh3UFN3hgk03NxohJpmQ6AZGtZqr_Y",
    "Content-Type": "application/json-patch+json"
}

data = {
    "id": "string",
    "userName": "user",
    "password": "user"
}

# verify=False is needed if the HTTPS cert is self-signed
response = requests.post(
    url,
    headers=headers,
    data=json.dumps(data),
    verify=False
)

print(response.status_code)
success = json.loads(response.text)['success']
print("success ? : ",success)
token = json.loads(response.text)['covenantToken']
# print(token)
print("-"*20)



url = "https://127.0.0.1:7443/api/users/current"
print("get : ",url)
headers = {
    "accept": "text/plain",
    "Authorization":  'Bearer '+ token
    }

# If the server uses a self-signed cert, keep verify=False
response = requests.get(url, headers=headers, verify=False)
print("status : ",response.status_code)
for key, value in response.headers.items():
    print(f"{key}: {value}")
print(response.text)