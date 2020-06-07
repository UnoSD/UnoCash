Use result monad for parameters' validation in functions

Investigate DevExpress Preview license for: https://demos.devexpress.com/blazor/

Blazor resources: https://github.com/AdrienTorris/awesome-blazor

Check sourcecode and try ORM: https://github.com/giometrix/TableStorage.Abstractions.TableEntityConverters

Disable temporarily VT switching for Rider CTRL+ALT+Fx: "setxkbmap -option srvrkeys:none"

Generate self-signed certificate:
    openssl genrsa 2048 > private.pem
    openssl req -x509 -new -key private.pem -out public.pem # Specify FQDN common name
    openssl pkcs12 -export -in public.pem -inkey private.pem -out mycert.pfx
    # Use PFX for APIM
    
Login URL
    https://login.microsoftonline.com/{{adId}}/oauth2/authorize?client_id={{appId}}&response_type=code&response_mode=query&prompt=admin_consent&resource_id={{appIdUri}}