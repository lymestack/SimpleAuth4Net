import os
import base64

def generate_token_secret(byte_length=64):
    # Generate a secure random byte array
    random_bytes = os.urandom(byte_length)
    # Encode the byte array to Base64
    token_secret = base64.b64encode(random_bytes).decode('utf-8')
    return token_secret

if __name__ == "__main__":
    print("Generating a secure TOKEN_SECRET...")
    secret = generate_token_secret()
    print(f"TOKEN_SECRET: {secret}")
    print("Copy the above TOKEN_SECRET and use it in your application.")
