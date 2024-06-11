# Keycloak Playground
This playground requires a specific realm in the Keycloak server.

## To setup your realm:
1. Run the application from the `Keycloak.AppHost` project dir
2. Browse to your Aspire dashboard
3. Browse to the Keycloak endpoint
4. Login with the admin credentials (found as environment variables for the Keycloak resource in the dashboard)
5. Once in the Keycloak Admin UI, open the Realms dropdown and click **Create realm**.
6. Browse to the **weathershop-realm.json** file at the root of this playground dir, and import it.
7. Your realm is ready to go!


## To register your user
User registration is enabled in the login page, so just browse to the webfrontend endpoint (from the Aspire dashboard), click **Login** and register your user from there.