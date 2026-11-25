// Recursos de F006 (research.md R2, R3 y R6), todos en niveles gratuitos permanentes:
// - Static Web App Free, que aloja la API como funciones gestionadas.
// - Cosmos DB for NoSQL con el nivel gratuito (1000 RU/s y 25 GB), sin superar 1000 RU/s.
// Todo lleva la etiqueta proyecto=hackmaster. La clave de Cosmos solo vive en Azure.
targetScope = 'resourceGroup'

@description('Región. Static Web Apps Free solo está en algunas; westeurope lo admite.')
param location string = 'westeurope'

@description('Prefijo de los nombres; tiene que ser único en Azure para la cuenta de Cosmos.')
param namePrefix string = 'hackmaster'

@description('Autoridad de Entra External ID (https://<inquilino>.ciamlogin.com/<id-del-inquilino>/v2.0).')
param authAuthority string

@description('Audiencia de los tokens: el identificador de la aplicación «HackMaster API».')
param authAudience string

@description('Orígenes que pueden llamar a la API, separados por comas (la dirección de GitHub Pages).')
param allowedOrigins string

@description('Identificadores (oid) de las cuentas de administración, separados por comas. Se pasa al desplegar.')
@secure()
param adminAccountIds string

var tags = {
  proyecto: 'hackmaster'
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: '${namePrefix}-datos'
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    enableFreeTier: true
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    backupPolicy: {
      type: 'Periodic'
      periodicModeProperties: {
        backupIntervalInMinutes: 1440
        backupRetentionIntervalInHours: 48
        backupStorageRedundancy: 'Local'
      }
    }
    minimalTlsVersion: 'Tls12'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-11-15' = {
  parent: cosmos
  name: 'hackmaster'
  tags: tags
  properties: {
    resource: {
      id: 'hackmaster'
    }
    options: {
      // Compartido por los dos contenedores; 1000 es todo el nivel gratuito.
      throughput: 1000
    }
  }
}

resource userData 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: 'userdata'
  tags: tags
  properties: {
    resource: {
      id: 'userdata'
      partitionKey: {
        paths: [
          '/userId'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource invitations 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-11-15' = {
  parent: database
  name: 'invitations'
  tags: tags
  properties: {
    resource: {
      id: 'invitations'
      partitionKey: {
        paths: [
          '/email'
        ]
        kind: 'Hash'
      }
    }
  }
}

resource site 'Microsoft.Web/staticSites@2024-04-01' = {
  name: '${namePrefix}-api'
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

resource siteSettings 'Microsoft.Web/staticSites/config@2024-04-01' = {
  parent: site
  name: 'appsettings'
  properties: {
    AUTH_AUTHORITY: authAuthority
    AUTH_AUDIENCE: authAudience
    ALLOWED_ORIGINS: allowedOrigins
    ADMIN_ACCOUNT_IDS: adminAccountIds
    COSMOS_ENDPOINT: cosmos.properties.documentEndpoint
    COSMOS_KEY: cosmos.listKeys().primaryMasterKey
    COSMOS_DATABASE: database.name
  }
}

output siteName string = site.name
output apiUrl string = 'https://${site.properties.defaultHostname}/'
