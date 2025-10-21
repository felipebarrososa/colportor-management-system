const { Client, MessageMedia } = require('whatsapp-web.js');
const qrcode = require('qrcode');
const express = require('express');
const cors = require('cors');
const multer = require('multer');
const fs = require('fs');
const path = require('path');

const app = express();
const PORT = process.env.PORT || 3001;

// Configurar multer para upload de arquivos
const upload = multer({
    storage: multer.memoryStorage(),
    limits: {
        fileSize: 50 * 1024 * 1024 // 50MB
    }
});

// Middleware
app.use(cors());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// Variáveis globais
let client = null;
let isConnected = false;
let qrCodeData = null;
let phoneNumber = null;
let lastConnection = null;
let reconnectAttempts = 0;
let maxReconnectAttempts = 5;
let reconnectInterval = null;
let connectionStatus = 'Disconnected';
let lastError = null;

// Configurações de reconexão
const RECONNECT_DELAYS = [5000, 10000, 30000, 60000, 300000]; // 5s, 10s, 30s, 1min, 5min

// Função para limpar sessão corrompida
function clearSession() {
    return new Promise((resolve) => {
        try {
            const sessionPath = path.join(__dirname, '.wwebjs_auth');
            if (fs.existsSync(sessionPath)) {
                // Aguardar um pouco antes de tentar remover
                setTimeout(() => {
                    try {
                        fs.rmSync(sessionPath, { recursive: true, force: true });
                        console.log('Sessão WhatsApp limpa com sucesso');
                        resolve(true);
                    } catch (error) {
                        console.log('Não foi possível limpar a sessão (pode estar em uso):', error.message);
                        resolve(false);
                    }
                }, 1000);
            } else {
                console.log('Nenhuma sessão encontrada para limpar');
                resolve(true);
            }
        } catch (error) {
            console.error('Erro ao limpar sessão:', error);
            resolve(false);
        }
    });
}

// Função para tentar reconectar
function scheduleReconnect() {
    if (reconnectAttempts >= maxReconnectAttempts) {
        console.log('Máximo de tentativas de reconexão atingido. Parando tentativas automáticas.');
        connectionStatus = 'Failed';
        return;
    }

    const delay = RECONNECT_DELAYS[Math.min(reconnectAttempts, RECONNECT_DELAYS.length - 1)];
    console.log(`Tentativa de reconexão ${reconnectAttempts + 1}/${maxReconnectAttempts} em ${delay/1000}s...`);
    
    reconnectInterval = setTimeout(() => {
        reconnectAttempts++;
        initializeWhatsApp();
    }, delay);
}

// Função para resetar tentativas de reconexão
function resetReconnectAttempts() {
    reconnectAttempts = 0;
    if (reconnectInterval) {
        clearTimeout(reconnectInterval);
        reconnectInterval = null;
    }
}

// Inicializar WhatsApp Client
async function initializeWhatsApp() {
    try {
        console.log('Iniciando conexão WhatsApp REAL...');
        console.log('Inicializando WhatsApp Client REAL...');
        
        // Limpar cliente anterior se existir
        if (client) {
            try {
                await client.destroy();
            } catch (error) {
                console.log('Cliente anterior já foi destruído');
            }
            client = null;
        }
        
        client = new Client({
            puppeteer: {
                headless: true,
                args: [
                    '--no-sandbox',
                    '--disable-setuid-sandbox',
                    '--disable-dev-shm-usage',
                    '--disable-accelerated-2d-canvas',
                    '--no-first-run',
                    '--no-zygote',
                    '--single-process',
                    '--disable-gpu',
                    '--disable-web-security',
                    '--disable-features=VizDisplayCompositor',
                    '--disable-extensions',
                    '--disable-plugins',
                    '--disable-default-apps',
                    '--disable-sync',
                    '--disable-translate',
                    '--hide-scrollbars',
                    '--mute-audio',
                    '--no-default-browser-check',
                    '--no-pings',
                    '--password-store=basic',
                    '--use-mock-keychain',
                    '--disable-background-timer-throttling',
                    '--disable-backgrounding-occluded-windows',
                    '--disable-renderer-backgrounding',
                    '--disable-features=TranslateUI',
                    '--disable-ipc-flooding-protection'
                ],
                timeout: 60000
            }
        });

        // Event listeners
        client.on('qr', async (qr) => {
            console.log('QR Code gerado');
            try {
                qrCodeData = await qrcode.toDataURL(qr);
                console.log('QR Code data URL gerado:', qrCodeData.substring(0, 100) + '...');
                connectionStatus = 'QR_Generated';
                lastError = null;
            } catch (error) {
                console.error('Erro ao gerar QR Code:', error);
                lastError = 'Erro ao gerar QR Code';
            }
        });

        client.on('ready', () => {
            console.log('WhatsApp Client REAL conectado com sucesso!');
            isConnected = true;
            connectionStatus = 'Connected';
            phoneNumber = client.info?.wid?.user || null;
            lastConnection = new Date();
            lastError = null;
            resetReconnectAttempts();
        });

        client.on('authenticated', (session) => {
            console.log('WhatsApp autenticado com sucesso!');
            connectionStatus = 'Authenticated';
            lastError = null;
        });

        client.on('auth_failure', (msg) => {
            console.error('Falha na autenticação WhatsApp:', msg);
            isConnected = false;
            connectionStatus = 'Auth_Failed';
            lastError = 'Falha na autenticação: ' + msg;
            clearSession().then(() => {
                scheduleReconnect();
            });
        });

        client.on('disconnected', (reason) => {
            console.log('WhatsApp desconectado:', reason);
            isConnected = false;
            connectionStatus = 'Disconnected';
            phoneNumber = null;
            lastConnection = null;
            
            if (reason === 'NAVIGATION') {
                console.log('Reconectando automaticamente...');
                scheduleReconnect();
            } else if (reason === 'LOGOUT') {
                console.log('Logout detectado. Limpando sessão...');
                clearSession().then(() => {
                    scheduleReconnect();
                });
            }
        });

        client.on('change_state', (state) => {
            console.log('Estado do WhatsApp mudou para:', state);
            connectionStatus = state;
        });

        // Inicializar cliente
        await client.initialize();
        
    } catch (error) {
        console.error('Erro ao inicializar WhatsApp Client:', error);
        lastError = 'Erro de inicialização: ' + error.message;
        connectionStatus = 'Error';
        scheduleReconnect();
    }
}

// Endpoint para obter status da conexão
app.get('/status', (req, res) => {
    res.json({
        connected: isConnected,
        phoneNumber: phoneNumber,
        lastConnection: lastConnection,
        qrCode: qrCodeData,
        status: connectionStatus,
        reconnectAttempts: reconnectAttempts,
        lastError: lastError
    });
});

// Endpoint para conectar
app.post('/connect', async (req, res) => {
    try {
        console.log('Solicitação de conexão recebida');
        resetReconnectAttempts();
        await clearSession();
        await initializeWhatsApp();
        res.json({ success: true, message: 'Iniciando conexão WhatsApp...' });
    } catch (error) {
        console.error('Erro ao conectar:', error);
        res.status(500).json({ success: false, message: 'Erro ao conectar: ' + error.message });
    }
});

// Endpoint para desconectar
app.post('/disconnect', async (req, res) => {
    try {
        console.log('Desconectando WhatsApp...');
        resetReconnectAttempts();
        
        if (client) {
            await client.destroy();
            client = null;
        }
        
        isConnected = false;
        phoneNumber = null;
        lastConnection = null;
        qrCodeData = null;
        connectionStatus = 'Disconnected';
        
        res.json({ success: true, message: 'WhatsApp desconectado com sucesso' });
    } catch (error) {
        console.error('Erro ao desconectar:', error);
        res.status(500).json({ success: false, message: 'Erro ao desconectar: ' + error.message });
    }
});

// Endpoint para limpar sessão
app.post('/clear-session', async (req, res) => {
    try {
        console.log('Limpando sessão WhatsApp...');
        resetReconnectAttempts();
        
        if (client) {
            await client.destroy();
            client = null;
        }
        
        await clearSession();
        
        isConnected = false;
        phoneNumber = null;
        lastConnection = null;
        qrCodeData = null;
        connectionStatus = 'Disconnected';
        
        res.json({ success: true, message: 'Sessão limpa com sucesso' });
    } catch (error) {
        console.error('Erro ao limpar sessão:', error);
        res.status(500).json({ success: false, message: 'Erro ao limpar sessão: ' + error.message });
    }
});

// Endpoint para enviar mensagem de texto
app.post('/send-message', async (req, res) => {
    try {
        const { phoneNumber, message } = req.body;
        
        if (!isConnected || !client) {
            return res.status(400).json({ 
                success: false, 
                message: 'WhatsApp não está conectado' 
            });
        }
        
        if (!phoneNumber || !message) {
            return res.status(400).json({ 
                success: false, 
                message: 'Número de telefone e mensagem são obrigatórios' 
            });
        }
        
        // Formatar número de telefone
        const formattedPhone = phoneNumber.includes('@c.us') 
            ? phoneNumber 
            : `${phoneNumber}@c.us`;
        
        console.log(`Enviando mensagem para: ${formattedPhone}`);
        console.log(`Conteúdo: ${message}`);
        
        const result = await client.sendMessage(formattedPhone, message);
        
        console.log(`Mensagem REAL enviada para ${formattedPhone}`);
        console.log(`ID da mensagem: ${result.id._serialized}`);
        
        res.json({
            success: true,
            messageId: result.id._serialized,
            message: 'Mensagem enviada com sucesso'
        });
        
    } catch (error) {
        console.error('Erro ao enviar mensagem de texto:', error);
        res.status(500).json({ 
            success: false, 
            message: 'Erro ao enviar mensagem: ' + error.message 
        });
    }
});

// Endpoint para enviar mídia
app.post('/send-media', upload.single('media'), async (req, res) => {
    try {
        const { phoneNumber, message, mediaType } = req.body;
        const mediaFile = req.file;
        
        if (!isConnected || !client) {
            return res.status(400).json({ 
                success: false, 
                message: 'WhatsApp não está conectado' 
            });
        }
        
        if (!phoneNumber || !mediaFile) {
            return res.status(400).json({ 
                success: false, 
                message: 'Número de telefone e arquivo de mídia são obrigatórios' 
            });
        }
        
        // Formatar número de telefone
        const formattedPhone = phoneNumber.includes('@c.us') 
            ? phoneNumber 
            : `${phoneNumber}@c.us`;
        
        console.log(`Enviando mídia para: ${formattedPhone}`);
        console.log(`Tipo de mídia: ${mediaType}`);
        console.log(`Arquivo: ${mediaFile.originalname}`);
        
        // Converter buffer para base64
        const mediaData = mediaFile.buffer.toString('base64');
        
        // Determinar mimetype e filename
        let finalMimetype = mediaFile.mimetype;
        let filename = mediaFile.originalname;
        
        // Ajustar mimetype baseado no tipo de mídia
        if (mediaType === 'audio') {
            // Para áudios, usar ogg
            finalMimetype = 'audio/ogg; codecs=opus';
            if (!filename.endsWith('.ogg')) {
                filename = filename.replace(/\.[^/.]+$/, '.ogg');
            }
        } else if (mediaType === 'image') {
            // Para imagens, manter o mimetype original
            finalMimetype = mediaFile.mimetype;
        } else if (mediaType === 'video') {
            // Para vídeos, usar mp4
            finalMimetype = 'video/mp4';
            if (!filename.endsWith('.mp4')) {
                filename = filename.replace(/\.[^/.]+$/, '.mp4');
            }
        }

        console.log(`Mimetype final: ${finalMimetype}`);
        console.log(`Filename final: ${filename}`);
        
        const messageMedia = new MessageMedia(finalMimetype, mediaData, filename);

        try {
            const result = await client.sendMessage(formattedPhone, message || '', { media: messageMedia });

            console.log(`Mídia REAL enviada para ${formattedPhone}`);
            console.log(`ID da mensagem: ${result.id._serialized}`);

            res.json({
                success: true,
                messageId: result.id._serialized,
                message: 'Mídia enviada com sucesso'
            });
        } catch (sendError) {
            console.error('Erro ao enviar mídia via WhatsApp:', sendError);
            
            // Fallback: enviar como mensagem de texto com emoji
            try {
                let fallbackMessage = message || '';
                let emoji = '';
                
                switch (mediaType) {
                    case 'audio':
                        emoji = '🎵';
                        break;
                    case 'image':
                        emoji = '🖼️';
                        break;
                    case 'video':
                        emoji = '🎥';
                        break;
                    default:
                        emoji = '📎';
                }
                
                const textResult = await client.sendMessage(formattedPhone, `${emoji} ${filename}${fallbackMessage ? '\n' + fallbackMessage : ''}`);
                console.log(`Mensagem de texto enviada como fallback: ${textResult.id._serialized}`);
                
                res.json({
                    success: true,
                    messageId: textResult.id._serialized,
                    message: 'Mídia enviada como mensagem de texto (fallback)'
                });
            } catch (fallbackError) {
                console.error('Erro no fallback:', fallbackError);
                res.status(500).json({ 
                    success: false, 
                    message: 'Erro ao enviar mídia: ' + sendError.message 
                });
            }
        }
        
    } catch (error) {
        console.error('Erro ao processar envio de mídia:', error);
        res.status(500).json({ 
            success: false, 
            message: 'Erro ao processar mídia: ' + error.message 
        });
    }
});

// Endpoint para obter mensagens
app.get('/messages/:phoneNumber', async (req, res) => {
    try {
        const { phoneNumber } = req.params;

        if (!isConnected || !client) {
            return res.status(400).json({
                success: false,
                message: 'WhatsApp não está conectado'
            });
        }

        // Obter o chatId correto usando a API do WhatsApp
        let chatId = null;
        try {
            const wid = await client.getNumberId(phoneNumber);
            chatId = wid?._serialized || null;
        } catch (e) {
            console.log('getNumberId falhou, usando fallback para @c.us:', e?.message);
        }
        if (!chatId) {
            chatId = phoneNumber.includes('@c.us') ? phoneNumber : `${phoneNumber}@c.us`;
        }

        console.log(`Buscando mensagens para chatId: ${chatId}`);

        // Tentar obter o chat diretamente; se falhar, procurar na lista de chats
        let chat = null;
        try {
            chat = await client.getChatById(chatId);
        } catch (e) {
            console.log('getChatById falhou, tentando localizar em getChats:', e?.message);
            const chats = await client.getChats();
            chat = chats.find(c => c?.id?._serialized === chatId || c?.id?.user === phoneNumber);
        }

        if (!chat) {
            console.log('Chat não encontrado para', chatId);
            return res.json({ success: true, messages: [] });
        }

        // Buscar últimas mensagens
        const messages = await chat.fetchMessages({ limit: 50 });

        const formattedMessages = await Promise.all(messages.map(async (msg) => {
            let mediaUrl = null;
            let mediaType = null;
            if (msg.hasMedia) {
                try {
                    const media = await msg.downloadMedia();
                    mediaUrl = `data:${media.mimetype};base64,${media.data}`;
                    mediaType = msg.type;
                } catch (e) {
                    console.log('Falha ao baixar mídia da mensagem:', e?.message);
                }
            }
            return {
                id: msg.id._serialized,
                content: msg.body || '',
                sender: msg.fromMe ? 'user' : 'contact',
                timestamp: new Date((msg.timestamp || Math.floor(Date.now()/1000)) * 1000),
                status: typeof msg.ack === 'number' ? msg.ack : 'unknown',
                mediaUrl,
                mediaType
            };
        }));

        console.log(`Encontradas ${formattedMessages.length} mensagens para ${chatId}`);

        res.json({ success: true, messages: formattedMessages });
    } catch (error) {
        console.error('Erro ao buscar mensagens:', error);
        res.status(500).json({
            success: false,
            message: 'Erro ao buscar mensagens: ' + error.message
        });
    }
});

// Inicializar servidor
app.listen(PORT, () => {
    console.log(`WhatsApp Service REAL rodando na porta ${PORT}`);
    initializeWhatsApp();
});

// Graceful shutdown
process.on('SIGINT', async () => {
    console.log('Encerrando WhatsApp Service...');
    if (client) {
        await client.destroy();
    }
    process.exit(0);
});

process.on('SIGTERM', async () => {
    console.log('Encerrando WhatsApp Service...');
    if (client) {
        await client.destroy();
    }
    process.exit(0);
});