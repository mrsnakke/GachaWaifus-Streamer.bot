const ts = () => new Date().toLocaleTimeString('es-MX', { hour12: false })

function log(tag, msg, ...rest) {
  const prefix = `[${ts()}] [${tag}]`
  if (rest.length) console.log(prefix, msg, ...rest)
  else console.log(prefix, msg)
}

function warn(tag, msg, ...rest) {
  const prefix = `[${ts()}] [${tag}]`
  if (rest.length) console.warn(prefix, msg, ...rest)
  else console.warn(prefix, msg)
}

function error(tag, msg, ...rest) {
  const prefix = `[${ts()}] [${tag}]`
  if (rest.length) console.error(prefix, msg, ...rest)
  else console.error(prefix, msg)
}

module.exports = { log, warn, error }
