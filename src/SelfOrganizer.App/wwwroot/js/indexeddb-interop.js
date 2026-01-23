// IndexedDB Interop for Blazor WebAssembly
const DB_NAME = "SelfOrganizerDb";
const DB_VERSION = 8; // Incremented to add taskReminderSnoozes

let db = null;

const stores = {
    captures: { keyPath: "id", indexes: ["createdAt", "isProcessed"] },
    tasks: { keyPath: "id", indexes: ["status", "projectId", "dueDate", "scheduledDate", "category"] },
    projects: { keyPath: "id", indexes: ["status", "category"] },
    events: { keyPath: "id", indexes: ["startTime", "endTime"] },
    timeBlocks: { keyPath: "id", indexes: ["startTime"] },
    contacts: { keyPath: "id", indexes: ["name"] },
    references: { keyPath: "id", indexes: ["category"] },
    contexts: { keyPath: "id", indexes: ["name"] },
    categories: { keyPath: "id", indexes: ["name"] },
    preferences: { keyPath: "id" },
    dailySnapshots: { keyPath: "id", indexes: ["date"] },
    goals: { keyPath: "id", indexes: ["status", "category", "targetDate"] },
    ideas: { keyPath: "id", indexes: ["status", "category", "linkedGoalId", "linkedProjectId"] },
    dataSourceConfigs: { keyPath: "id", indexes: ["sourceType", "isEnabled"] },
    syncJobs: { keyPath: "id", indexes: ["sourceType", "status", "createdAt"] },
    habits: { keyPath: "id", indexes: ["isActive", "category"] },
    habitLogs: { keyPath: "id", indexes: ["habitId", "date"] },
    weeklySnapshots: { keyPath: "id", indexes: ["weekStart"] },
    entityLinkRules: { keyPath: "id", indexes: ["targetType", "isEnabled", "priority"] },
    focusSessionLogs: { keyPath: "id", indexes: ["taskId", "startedAt", "endedAt", "focusRating"] },
    taskReminderSnoozes: { keyPath: "id", indexes: ["taskId", "snoozedUntil"] }
};

window.indexedDbInterop = {
    initialize: async function () {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(DB_NAME, DB_VERSION);

            request.onerror = (event) => {
                console.error("IndexedDB open error:", event.target.error);
                reject(request.error);
            };

            request.onsuccess = () => {
                db = request.result;

                // Handle version change from other tabs
                db.onversionchange = () => {
                    db.close();
                    console.log("Database version changed, please reload");
                };

                resolve(true);
            };

            // Handle blocked upgrade (another tab has db open)
            request.onblocked = (event) => {
                console.warn("IndexedDB upgrade blocked - close other tabs with this app");
                // Still resolve after a delay to allow app to work
                setTimeout(() => {
                    if (db) resolve(true);
                }, 1000);
            };

            request.onupgradeneeded = (event) => {
                const database = event.target.result;
                const transaction = event.target.transaction;

                // Handle upgrade errors
                transaction.onerror = (e) => {
                    console.error("IndexedDB upgrade transaction error:", e.target.error);
                };

                transaction.onabort = (e) => {
                    console.error("IndexedDB upgrade aborted:", e.target.error);
                };

                for (const [storeName, config] of Object.entries(stores)) {
                    if (!database.objectStoreNames.contains(storeName)) {
                        try {
                            const store = database.createObjectStore(storeName, { keyPath: config.keyPath });
                            if (config.indexes) {
                                config.indexes.forEach(indexName => {
                                    store.createIndex(indexName, indexName, { unique: false });
                                });
                            }
                        } catch (e) {
                            console.error(`Failed to create store ${storeName}:`, e);
                        }
                    }
                }
            };
        });
    },

    add: async function (storeName, item) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readwrite");
            const store = transaction.objectStore(storeName);
            const request = store.add(item);

            request.onsuccess = () => resolve(item);
            request.onerror = () => reject(request.error);
        });
    },

    put: async function (storeName, item) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readwrite");
            const store = transaction.objectStore(storeName);
            const request = store.put(item);

            request.onsuccess = () => resolve(item);
            request.onerror = () => reject(request.error);
        });
    },

    get: async function (storeName, id) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);
            const request = store.get(id);

            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    },

    getAll: async function (storeName) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);
            const request = store.getAll();

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    delete: async function (storeName, id) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readwrite");
            const store = transaction.objectStore(storeName);
            const request = store.delete(id);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    clear: async function (storeName) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readwrite");
            const store = transaction.objectStore(storeName);
            const request = store.clear();

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    },

    count: async function (storeName) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);
            const request = store.count();

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    getByIndex: async function (storeName, indexName, value) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);
            const index = store.index(indexName);
            const request = index.getAll(value);

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    getByIndexRange: async function (storeName, indexName, lower, upper) {
        return new Promise((resolve, reject) => {
            const transaction = db.transaction([storeName], "readonly");
            const store = transaction.objectStore(storeName);
            const index = store.index(indexName);
            const range = IDBKeyRange.bound(lower, upper);
            const request = index.getAll(range);

            request.onsuccess = () => resolve(request.result);
            request.onerror = () => reject(request.error);
        });
    },

    exportAll: async function () {
        const data = {};
        for (const storeName of Object.keys(stores)) {
            data[storeName] = await this.getAll(storeName);
        }
        return JSON.stringify(data);
    },

    importAll: async function (jsonData) {
        const data = JSON.parse(jsonData);
        for (const [storeName, items] of Object.entries(data)) {
            if (stores[storeName]) {
                await this.clear(storeName);
                for (const item of items) {
                    await this.add(storeName, item);
                }
            }
        }
        return true;
    },

    deleteDatabase: async function () {
        db.close();
        return new Promise((resolve, reject) => {
            const request = indexedDB.deleteDatabase(DB_NAME);
            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }
};
