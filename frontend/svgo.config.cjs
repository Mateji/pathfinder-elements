module.exports = {
    multipass: true,
    plugins: [
        {
            name: "preset-default",
            params: {
                overrides: {
                    removeViewBox: false,
                },
            },
        },
        {
            name: "removeAttrs",
            params: {
                attrs: "(fill|stroke|style|class)",
            },
        },
    ],
};