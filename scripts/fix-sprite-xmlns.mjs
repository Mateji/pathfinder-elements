import fs from "node:fs";

const spriteFilePath = "src/assets/icons.svg";

let fileText = fs.readFileSync(spriteFilePath, "utf8");

fileText = fileText.replace(/<svg([^>]*)>/, (_match, attributesText) => {
    let updatedAttributesText = attributesText;

    if (!/xmlns=/.test(updatedAttributesText)) {
        updatedAttributesText += ' xmlns="http://www.w3.org/2000/svg"';
    }

    if (!/xmlns:xlink=/.test(updatedAttributesText)) {
        updatedAttributesText += ' xmlns:xlink="http://www.w3.org/1999/xlink"';
    }

    return `<svg${updatedAttributesText}>`;
});

fs.writeFileSync(spriteFilePath, fileText);
console.log("✅ icons.svg updated with xmlns attributes");