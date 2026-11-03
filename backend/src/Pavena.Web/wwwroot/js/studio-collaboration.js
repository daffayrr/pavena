// Pavena Research Studio — Monaco Editor & Real-Time Collaboration Bridge
(function () {
    window.PavenaStudio = {
        editor: null,
        models: {},
        dotNetHelper: null,
        hubConnection: null,
        currentFileId: null,
        currentDocumentId: null,
        isApplyingRemoteUpdate: false,
        activeTheme: 'vs',

        initEditor: function (containerId, initialContent, language, dotNetHelper, isDark) {
            this.dotNetHelper = dotNetHelper;
            this.activeTheme = isDark ? 'vs-dark' : 'vs';

            var container = document.getElementById(containerId);
            if (!container) return;

            // Check if Monaco is already loaded
            if (window.monaco) {
                this.setupMonaco(container, initialContent, language);
            } else {
                this.loadMonaco(container, initialContent, language);
            }
        },

        loadMonaco: function (container, initialContent, language) {
            var self = this;
            if (!window.require) {
                var script = document.createElement('script');
                script.src = 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/loader.min.js';
                script.onload = function () {
                    window.require.config({ paths: { vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' } });
                    window.require(['vs/editor/editor.main'], function () {
                        self.setupMonaco(container, initialContent, language);
                    });
                };
                script.onerror = function () {
                    console.warn('[PavenaStudio] Monaco CDN unavailable, falling back to enhanced editor.');
                    self.setupFallbackEditor(container, initialContent);
                };
                document.head.appendChild(script);
            } else {
                window.require(['vs/editor/editor.main'], function () {
                    self.setupMonaco(container, initialContent, language);
                });
            }
        },

        setupMonaco: function (container, initialContent, language) {
            var self = this;
            container.innerHTML = '';

            // Register LaTeX language config if needed
            var lang = language || 'latex';
            if (lang === 'tex' || lang === 'bib') lang = 'latex';

            self.editor = monaco.editor.create(container, {
                value: initialContent || '',
                language: lang,
                theme: self.activeTheme,
                automaticLayout: true,
                fontSize: 14,
                lineHeight: 22,
                fontFamily: "'JetBrains Mono', 'Fira Code', Consolas, monospace",
                minimap: { enabled: true, maxColumn: 80 },
                scrollBeyondLastLine: false,
                wordWrap: 'on',
                renderWhitespace: 'selection',
                tabSize: 2,
                insertSpaces: true
            });

            // Content change handler
            self.editor.onDidChangeModelContent(function (e) {
                if (self.isApplyingRemoteUpdate) return;
                var text = self.editor.getValue();
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnEditorContentChanged', text);
                }
            });

            // Cursor selection change for presence awareness
            self.editor.onDidChangeCursorPosition(function (e) {
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnCursorPositionChanged', e.position.lineNumber, e.position.column);
                }
            });

            // Keybindings: Ctrl+S to save, Ctrl+Enter to compile
            self.editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, function () {
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnSaveShortcutTriggered');
                }
            });

            self.editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, function () {
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnCompileShortcutTriggered');
                }
            });
        },

        setupFallbackEditor: function (container, initialContent) {
            var self = this;
            container.innerHTML = '<textarea id="pavena-fallback-editor" class="pavena-studio-fallback-area" spellcheck="false">' + (initialContent || '') + '</textarea>';
            var area = document.getElementById('pavena-fallback-editor');
            area.addEventListener('input', function () {
                if (self.dotNetHelper) {
                    self.dotNetHelper.invokeMethodAsync('OnEditorContentChanged', area.value);
                }
            });
            area.addEventListener('keydown', function (e) {
                if ((e.ctrlKey || e.metaKey) && e.key === 's') {
                    e.preventDefault();
                    if (self.dotNetHelper) self.dotNetHelper.invokeMethodAsync('OnSaveShortcutTriggered');
                }
                if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                    e.preventDefault();
                    if (self.dotNetHelper) self.dotNetHelper.invokeMethodAsync('OnCompileShortcutTriggered');
                }
            });
        },

        switchFileModel: function (fileId, content, language) {
            this.currentFileId = fileId;
            var lang = language || 'latex';
            if (lang === 'tex' || lang === 'bib') lang = 'latex';

            if (this.editor && window.monaco) {
                this.isApplyingRemoteUpdate = true;
                var model = this.models[fileId];
                if (!model) {
                    model = monaco.editor.createModel(content, lang);
                    this.models[fileId] = model;
                } else {
                    if (model.getValue() !== content) {
                        model.setValue(content);
                    }
                }
                this.editor.setModel(model);
                this.isApplyingRemoteUpdate = false;
            } else {
                var area = document.getElementById('pavena-fallback-editor');
                if (area) area.value = content || '';
            }
        },

        updateContent: function (content) {
            if (this.editor && window.monaco) {
                var current = this.editor.getValue();
                if (current !== content) {
                    this.isApplyingRemoteUpdate = true;
                    var pos = this.editor.getPosition();
                    this.editor.setValue(content || '');
                    if (pos) this.editor.setPosition(pos);
                    this.isApplyingRemoteUpdate = false;
                }
            } else {
                var area = document.getElementById('pavena-fallback-editor');
                if (area && area.value !== content) {
                    area.value = content || '';
                }
            }
        },

        getContent: function () {
            if (this.editor && window.monaco) {
                return this.editor.getValue();
            }
            var area = document.getElementById('pavena-fallback-editor');
            return area ? area.value : '';
        },

        setTheme: function (isDark) {
            this.activeTheme = isDark ? 'vs-dark' : 'vs';
            if (window.monaco && this.editor) {
                monaco.editor.setTheme(this.activeTheme);
            }
        },

        renderRemoteCursor: function (userId, userName, line, col, color) {
            // Render collaborator presence flag in editor
            if (!this.editor || !window.monaco) return;
            // Visual decorations can be added here
        },

        dispose: function () {
            if (this.editor) {
                this.editor.dispose();
                this.editor = null;
            }
            this.models = {};
        }
    };
})();
