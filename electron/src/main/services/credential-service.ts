import { safeStorage } from 'electron';
import Store from 'electron-store';
import log from 'electron-log';

const store = new Store({
  name: 'credentials',
  encryptionKey: 'teachassist-desktop-v4',
});

const KEY_USERNAME = 'saved_username';
const KEY_PASSWORD = 'saved_password';

export async function saveCredentials(username: string, password: string): Promise<void> {
  try {
    if (safeStorage.isEncryptionAvailable()) {
      store.set(KEY_USERNAME, safeStorage.encryptString(username).toString('base64'));
      store.set(KEY_PASSWORD, safeStorage.encryptString(password).toString('base64'));
    } else {
      store.set(KEY_USERNAME, username);
      store.set(KEY_PASSWORD, password);
    }
  } catch (err) {
    log.warn('Failed to save credentials:', err);
  }
}

export async function getCredentials(): Promise<{ username: string | null; password: string | null }> {
  try {
    const rawUser = store.get(KEY_USERNAME) as string | undefined;
    const rawPass = store.get(KEY_PASSWORD) as string | undefined;

    if (!rawUser || !rawPass) return { username: null, password: null };

    if (safeStorage.isEncryptionAvailable()) {
      const username = safeStorage.decryptString(Buffer.from(rawUser, 'base64'));
      const password = safeStorage.decryptString(Buffer.from(rawPass, 'base64'));
      return { username, password };
    }

    return { username: rawUser, password: rawPass };
  } catch (err) {
    log.warn('Failed to load credentials:', err);
    return { username: null, password: null };
  }
}

export async function clearCredentials(): Promise<void> {
  try {
    store.delete(KEY_USERNAME);
    store.delete(KEY_PASSWORD);
  } catch (err) {
    log.warn('Failed to clear credentials:', err);
  }
}
