/**
 * Post-build script to copy webpack output into AEM clientlib folders.
 * This ensures the React JS/CSS and vendor bundle are available as AEM ClientLibraries.
 * Works cross-platform (Windows, macOS, Linux).
 */
const fs = require('fs');
const path = require('path');

const distDir = path.resolve(__dirname, 'dist');
const clientlibsBase = path.resolve(__dirname, '..', 'ui.apps', 'src', 'main', 'content',
    'jcr_root', 'apps', 'devinreactaem', 'clientlibs');

const copies = [
    { from: 'js/react-app.js', toDir: 'clientlib-react/js' },
    { from: 'css/react-app.css', toDir: 'clientlib-react/css' },
    { from: 'js/vendor.js', toDir: 'clientlib-dependencies/js' },
];

let copied = 0;
copies.forEach(({ from, toDir }) => {
    const src = path.join(distDir, from);
    const destDir = path.join(clientlibsBase, toDir);
    const dest = path.join(destDir, path.basename(from));

    if (!fs.existsSync(src)) {
        console.log(`  skip: ${from} (not found in dist)`);
        return;
    }

    fs.mkdirSync(destDir, { recursive: true });
    fs.copyFileSync(src, dest);
    console.log(`  copy: dist/${from} -> clientlibs/${toDir}/${path.basename(from)}`);
    copied++;
});

console.log(`sync-clientlibs: ${copied} file(s) copied to AEM clientlibs.`);
