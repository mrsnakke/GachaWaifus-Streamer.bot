// ponytail: receives forwarded events from the Kick backend
const { Router } = require('express')
const bus = require('../lib/event-bus')
const logger = require('../lib/logger')

const TAG = 'WEBHOOK'
const router = Router()

router.post('/kick-events', (req, res) => {
  const { event, data } = req.body
  if (!event) return res.status(400).json({ error: 'Missing event field' })

  logger.log(TAG, `Received event: ${event}`)
  bus.emit(event, data || {})

  res.json({ ok: true })
})

// health check
router.get('/health', (req, res) => {
  res.json({ status: 'ok', uptime: process.uptime() })
})

module.exports = router
