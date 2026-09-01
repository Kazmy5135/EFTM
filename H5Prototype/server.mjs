import { createServer } from "node:http";
import { readFile, stat } from "node:fs/promises";
import { extname, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { networkInterfaces } from "node:os";

const root = resolve(fileURLToPath(new URL(".", import.meta.url)));
const port = Number.parseInt(process.env.EFTM_H5_PORT || "4173", 10);
const host = process.env.EFTM_H5_HOST || "0.0.0.0";
const mimeTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".svg": "image/svg+xml"
};

createServer(async (request, response) => {
  try {
    const requestUrl = new URL(request.url || "/", "http://127.0.0.1");
    const requestedPath = decodeURIComponent(requestUrl.pathname === "/" ? "/index.html" : requestUrl.pathname);
    if (requestedPath.startsWith("/node_modules/")) {
      response.writeHead(403).end("Forbidden");
      return;
    }
    const absolutePath = resolve(root, `.${requestedPath}`);

    const isPrototypeFile = absolutePath === root || absolutePath.startsWith(`${root}${sep}`);
    if (!isPrototypeFile) {
      response.writeHead(403).end("Forbidden");
      return;
    }

    const fileStat = await stat(absolutePath);
    if (!fileStat.isFile()) {
      response.writeHead(404).end("Not found");
      return;
    }

    const body = await readFile(absolutePath);
    response.writeHead(200, {
      "Cache-Control": "no-store",
      "Content-Type": mimeTypes[extname(absolutePath)] || "application/octet-stream"
    });
    response.end(body);
  } catch {
    response.writeHead(404).end("Not found");
  }
}).listen(port, host, () => {
  process.stdout.write(`Local: http://127.0.0.1:${port}/\n`);
  for (const addresses of Object.values(networkInterfaces())) {
    for (const address of addresses || []) {
      if (address.family === "IPv4" && !address.internal) {
        process.stdout.write(`LAN:   http://${address.address}:${port}/\n`);
      }
    }
  }
});
