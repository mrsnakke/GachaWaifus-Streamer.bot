// ponytail: sends messages back to Kick chat via the Kick backend's API
const config = require('../../lib/config')
const logger = require('../../lib/logger')

const TAG = 'CHAT'

async function send(message) {
  const url = `${config.kickBackendUrl}/api/chat/send`
  try {
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ content: message }),
    })
    if (!res.ok) logger.warn(TAG, `Chat send returned ${res.status}`)
    else logger.log(TAG, `Sent: ${message.slice(0, 80)}`)
  } catch (e) {
    logger.error(TAG, `Failed to send chat message: ${e.message}`)
  }
}

module.exports = { send }
