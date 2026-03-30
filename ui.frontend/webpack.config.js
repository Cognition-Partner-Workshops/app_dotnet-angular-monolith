const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const CopyWebpackPlugin = require('copy-webpack-plugin');

module.exports = (env, argv) => {
    const isProduction = argv.mode === 'production';

    return {
        entry: {
            'react-app': './src/index.tsx',
        },
        output: {
            filename: 'js/[name].js',
            path: path.resolve(__dirname, 'dist'),
            clean: true,
        },
        resolve: {
            extensions: ['.ts', '.tsx', '.js', '.jsx'],
            alias: {
                '@components': path.resolve(__dirname, 'src/components/'),
                '@hooks': path.resolve(__dirname, 'src/hooks/'),
                '@context': path.resolve(__dirname, 'src/context/'),
                '@utils': path.resolve(__dirname, 'src/utils/'),
            },
        },
        module: {
            rules: [
                {
                    test: /\.tsx?$/,
                    use: 'ts-loader',
                    exclude: /node_modules/,
                },
                {
                    test: /\.css$/,
                    use: [
                        isProduction ? MiniCssExtractPlugin.loader : 'style-loader',
                        'css-loader',
                    ],
                },
            ],
        },
        plugins: [
            new MiniCssExtractPlugin({
                filename: 'css/[name].css',
            }),
            new CopyWebpackPlugin({
                patterns: [
                    {
                        from: path.resolve(__dirname, 'dist'),
                        to: path.resolve(
                            __dirname,
                            '../ui.apps/src/main/content/jcr_root/apps/devinreactaem/clientlibs/clientlib-react'
                        ),
                        noErrorOnMissing: true,
                    },
                ],
            }),
        ],
        devServer: {
            port: 3000,
            hot: true,
            proxy: {
                '/content': {
                    target: 'http://localhost:4502',
                    changeOrigin: true,
                },
                '/bin': {
                    target: 'http://localhost:4502',
                    changeOrigin: true,
                },
            },
        },
        devtool: isProduction ? 'source-map' : 'eval-source-map',
        optimization: {
            splitChunks: {
                cacheGroups: {
                    vendor: {
                        test: /[\\/]node_modules[\\/]/,
                        name: 'vendor',
                        chunks: 'all',
                    },
                },
            },
        },
    };
};
