const port = process.env.PATHFINDER_API_PORT ?? '5080';

if (!/^\d{1,5}$/.test(port) || Number(port) < 1 || Number(port) > 65535) {
    throw new Error('PATHFINDER_API_PORT must be a valid port number.');
}

module.exports = {
    '/api/**': {
        target: `http://127.0.0.1:${port}`,
        xfwd: true
    }
};
