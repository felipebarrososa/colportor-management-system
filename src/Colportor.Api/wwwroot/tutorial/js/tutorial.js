// Tutorial JavaScript - Funcionalidades interativas

class TutorialManager {
    constructor() {
        this.currentSection = null;
        this.searchResults = [];
        this.init();
    }

    init() {
        this.setupSearch();
        this.setupNavigation();
        this.setupScrollSpy();
        this.setupPrintFunctionality();
        this.setupDarkMode();
    }

    // Sistema de busca
    setupSearch() {
        const searchBox = document.getElementById('searchBox');
        if (!searchBox) return;

        searchBox.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase();
            this.searchContent(query);
        });
    }

    searchContent(query) {
        if (!query.trim()) {
            this.clearSearchResults();
            return;
        }

        const sections = document.querySelectorAll('.tutorial-section');
        const results = [];

        sections.forEach(section => {
            const title = section.querySelector('h2, h3')?.textContent.toLowerCase() || '';
            const content = section.textContent.toLowerCase();
            
            if (title.includes(query) || content.includes(query)) {
                results.push({
                    element: section,
                    title: section.querySelector('h2, h3')?.textContent || 'Seção',
                    relevance: title.includes(query) ? 2 : 1
                });
            }
        });

        this.displaySearchResults(results);
    }

    displaySearchResults(results) {
        const resultsContainer = document.getElementById('searchResults');
        if (!resultsContainer) return;

        if (results.length === 0) {
            resultsContainer.innerHTML = '<p>Nenhum resultado encontrado.</p>';
            return;
        }

        results.sort((a, b) => b.relevance - a.relevance);
        
        const html = results.map(result => `
            <div class="search-result" onclick="tutorialManager.scrollToSection('${result.element.id}')">
                <h4>${result.title}</h4>
                <p>Clique para ir para esta seção</p>
            </div>
        `).join('');

        resultsContainer.innerHTML = html;
    }

    clearSearchResults() {
        const resultsContainer = document.getElementById('searchResults');
        if (resultsContainer) {
            resultsContainer.innerHTML = '';
        }
    }

    // Navegação suave
    setupNavigation() {
        const navLinks = document.querySelectorAll('.toc a, .nav-card');
        navLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const targetId = link.getAttribute('href')?.substring(1);
                if (targetId) {
                    this.scrollToSection(targetId);
                }
            });
        });
    }

    scrollToSection(sectionId) {
        const section = document.getElementById(sectionId);
        if (section) {
            section.scrollIntoView({ 
                behavior: 'smooth',
                block: 'start'
            });
            
            // Destacar seção temporariamente
            section.style.transition = 'background-color 0.3s ease';
            section.style.backgroundColor = '#fff3cd';
            setTimeout(() => {
                section.style.backgroundColor = '';
            }, 2000);
        }
    }

    // Scroll spy para destacar seção atual
    setupScrollSpy() {
        const sections = document.querySelectorAll('.tutorial-section');
        const navLinks = document.querySelectorAll('.toc a');

        window.addEventListener('scroll', () => {
            let current = '';
            
            sections.forEach(section => {
                const sectionTop = section.offsetTop;
                const sectionHeight = section.clientHeight;
                if (window.pageYOffset >= sectionTop - 200) {
                    current = section.getAttribute('id');
                }
            });

            navLinks.forEach(link => {
                link.classList.remove('active');
                if (link.getAttribute('href') === `#${current}`) {
                    link.classList.add('active');
                }
            });
        });
    }

    // Funcionalidade de impressão
    setupPrintFunctionality() {
        const printButton = document.getElementById('printButton');
        if (printButton) {
            printButton.addEventListener('click', () => {
                this.printTutorial();
            });
        }
    }

    printTutorial() {
        const printWindow = window.open('', '_blank');
        const content = document.querySelector('.tutorial-content').innerHTML;
        
        printWindow.document.write(`
            <html>
                <head>
                    <title>Tutorial - Colportor ID</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        h1, h2, h3 { color: #333; }
                        .step-card { background: #f8f9fa; padding: 15px; margin: 10px 0; border-left: 3px solid #667eea; }
                        .feature-grid { display: block; }
                        .feature-item { margin: 10px 0; padding: 15px; border: 1px solid #ddd; }
                        .code-block { background: #f5f5f5; padding: 15px; font-family: monospace; }
                        .highlight, .warning, .success { padding: 15px; margin: 10px 0; }
                    </style>
                </head>
                <body>
                    <h1>Tutorial - Colportor ID</h1>
                    ${content}
                </body>
            </html>
        `);
        
        printWindow.document.close();
        printWindow.print();
    }

    // Modo escuro
    setupDarkMode() {
        const darkModeToggle = document.getElementById('darkModeToggle');
        if (darkModeToggle) {
            darkModeToggle.addEventListener('click', () => {
                this.toggleDarkMode();
            });
        }

        // Verificar preferência salva
        const savedTheme = localStorage.getItem('tutorial-theme');
        if (savedTheme === 'dark') {
            document.body.classList.add('dark-mode');
        }
    }

    toggleDarkMode() {
        document.body.classList.toggle('dark-mode');
        const isDark = document.body.classList.contains('dark-mode');
        localStorage.setItem('tutorial-theme', isDark ? 'dark' : 'light');
    }

    // Expandir/colapsar seções
    toggleSection(sectionId) {
        const section = document.getElementById(sectionId);
        const content = section.querySelector('.section-content');
        const toggle = section.querySelector('.section-toggle');
        
        if (content.style.display === 'none') {
            content.style.display = 'block';
            toggle.textContent = '▼';
        } else {
            content.style.display = 'none';
            toggle.textContent = '▶';
        }
    }

    // Copiar código para clipboard
    copyCode(codeElement) {
        const text = codeElement.textContent;
        navigator.clipboard.writeText(text).then(() => {
            this.showNotification('Código copiado!', 'success');
        }).catch(() => {
            this.showNotification('Erro ao copiar código', 'error');
        });
    }

    // Mostrar notificações
    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;
        
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 5px;
            color: white;
            font-weight: 500;
            z-index: 1000;
            animation: slideIn 0.3s ease;
        `;

        if (type === 'success') {
            notification.style.backgroundColor = '#28a745';
        } else if (type === 'error') {
            notification.style.backgroundColor = '#dc3545';
        } else {
            notification.style.backgroundColor = '#17a2b8';
        }

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => {
                document.body.removeChild(notification);
            }, 300);
        }, 3000);
    }

    // Animações CSS
    addAnimations() {
        const style = document.createElement('style');
        style.textContent = `
            @keyframes slideIn {
                from { transform: translateX(100%); opacity: 0; }
                to { transform: translateX(0); opacity: 1; }
            }
            
            @keyframes slideOut {
                from { transform: translateX(0); opacity: 1; }
                to { transform: translateX(100%); opacity: 0; }
            }
            
            .toc a.active {
                color: #667eea;
                font-weight: bold;
            }
            
            .search-result {
                background: #f8f9fa;
                padding: 15px;
                margin: 10px 0;
                border-radius: 8px;
                cursor: pointer;
                transition: all 0.3s ease;
            }
            
            .search-result:hover {
                background: #e9ecef;
                transform: translateX(5px);
            }
        `;
        document.head.appendChild(style);
    }
}

// Inicializar quando o DOM estiver carregado
document.addEventListener('DOMContentLoaded', () => {
    window.tutorialManager = new TutorialManager();
    window.tutorialManager.addAnimations();
});

// Funções globais para uso nos templates
function scrollToSection(sectionId) {
    if (window.tutorialManager) {
        window.tutorialManager.scrollToSection(sectionId);
    }
}

function toggleSection(sectionId) {
    if (window.tutorialManager) {
        window.tutorialManager.toggleSection(sectionId);
    }
}

function copyCode(codeElement) {
    if (window.tutorialManager) {
        window.tutorialManager.copyCode(codeElement);
    }
}
