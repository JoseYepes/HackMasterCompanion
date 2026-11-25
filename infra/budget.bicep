// Presupuesto de la suscripción (F006, constitución VII). Se despliega antes que nada más.
// Avisa por correo al gastar el 10 % del presupuesto y si se prevé llegar al 100 %. No para el
// consumo: solo avisa (research.md R6).
targetScope = 'subscription'

@description('Correo que recibe los avisos. Se pasa al desplegar; no se guarda en el repositorio.')
param contactEmail string

@description('Primer día del mes en que empieza el presupuesto (AAAA-MM-01).')
param startDate string

@description('Importe mensual en la moneda de la suscripción.')
param amount int = 1

resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: 'hackmaster-presupuesto'
  properties: {
    category: 'Cost'
    amount: amount
    timeGrain: 'Monthly'
    timePeriod: {
      startDate: startDate
    }
    notifications: {
      real10: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 10
        thresholdType: 'Actual'
        contactEmails: [
          contactEmail
        ]
      }
      previsto100: {
        enabled: true
        operator: 'GreaterThan'
        threshold: 100
        thresholdType: 'Forecasted'
        contactEmails: [
          contactEmail
        ]
      }
    }
  }
}
