const config = require('/web/webpack.prod.js');
const MinimizerPlugin = require('/web/node_modules/minimizer-webpack-plugin');

module.exports = {
    ...config,
    optimization: {
        ...config.optimization,
        minimizer: [new MinimizerPlugin({
            parallel: 2,
            terserOptions: { compress: { passes: 2 } }
        })]
    }
};
