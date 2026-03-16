import fs from 'fs';
import path from 'path';
import { app } from 'electron';

const DEFAULT_DIR = () => path.join(app.getPath('userData'), 'data');

function readJsonFile<T>(filePath: string): T | null {
  try {
    if (!fs.existsSync(filePath)) return null;
    const raw = fs.readFileSync(filePath, 'utf-8');
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
}

function writeJsonFile(filePath: string, data: unknown): void {
  const dir = path.dirname(filePath);
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2), 'utf-8');
}

/** Simple JSON file store — replaces electron-store */
export class JsonStore {
  private data: Record<string, unknown>;
  private filePath: string;

  constructor(name: string, defaults: Record<string, unknown> = {}) {
    this.filePath = path.join(DEFAULT_DIR(), `${name}.json`);
    this.data = { ...defaults, ...(readJsonFile<Record<string, unknown>>(this.filePath) || {}) };
  }

  get<T = unknown>(key: string, defaultValue?: T): T {
    if (key in this.data) return this.data[key] as T;
    if (defaultValue !== undefined) return defaultValue;
    return undefined as T;
  }

  set(key: string, value: unknown): void {
    this.data[key] = value;
    this.save();
  }

  delete(key: string): void {
    delete this.data[key];
    this.save();
  }

  private save(): void {
    writeJsonFile(this.filePath, this.data);
  }
}
