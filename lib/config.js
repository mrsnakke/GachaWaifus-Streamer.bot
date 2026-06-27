const path = require('path')
require('dotenv').config({ path: path.join(__dirname, '..', '.env') })

const config = {
  port: parseInt(process.env.PORT, 10) || 8085,
  // ponytail: Kick backend URL, add multiple URLs if needed
  kickBackendUrl: process.env.KICK_BACKEND_URL || 'http://localhost:3000',
  githubToken: process.env.GITHUB_TOKEN || '',
  github: {
    owner: 'mrsnakke',
    repo: 'gachaIMG',
    branch: 'main',
  },
  dataDir: path.join(__dirname, '..', 'GachaWish'),
  webDir: path.join(__dirname, '..', 'web'),
}

module.exports = config
