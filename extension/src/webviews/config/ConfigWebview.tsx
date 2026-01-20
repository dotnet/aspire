// @ts-nocheck - This file is compiled with Babel (ESNext modules), not ts-loader
import React, { useState, useCallback } from 'react';
import { VSCodeButton, VSCodeTextField, VSCodeCheckbox, VSCodeDataGrid, VSCodeDataGridRow, VSCodeDataGridCell, VSCodeDivider } from '@vscode/webview-ui-toolkit/react';
import { l10n } from '../l10n';
import { usePostMessage, useDataRequest } from '../hooks';

interface ConfigItem {
  key: string;
  value: string;
  isGlobal: boolean;
}

export const ConfigWebview: React.FC = () => {
  const [configItems, setConfigItems] = useState<ConfigItem[]>([]);
  const [localSettingsPath, setLocalSettingsPath] = useState<string | null>(null);
  const [globalSettingsPath, setGlobalSettingsPath] = useState<string | null>(null);
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState<string>('');
  const [editIsGlobal, setEditIsGlobal] = useState<boolean>(false);
  const [isAddingNew, setIsAddingNew] = useState<boolean>(false);
  const [newKey, setNewKey] = useState<string>('');
  const [newValue, setNewValue] = useState<string>('');
  const [newIsGlobal, setNewIsGlobal] = useState<boolean>(false);

  // Request config on mount and listen for updates
  useDataRequest('getConfig', 'configData', (data, metadata) => {
    const items: ConfigItem[] = Object.entries(data).map(([key, info]: [string, any]) => ({
      key,
      value: info.value,
      isGlobal: info.isGlobal
    }));
    setConfigItems(items);
    
    if (metadata) {
      setLocalSettingsPath(metadata.localSettingsPath);
      setGlobalSettingsPath(metadata.globalSettingsPath);
    }
  });

  const updateConfig = usePostMessage('updateConfig');
  const deleteConfig = usePostMessage('deleteConfig');

  const handleEdit = (item: ConfigItem) => {
    setEditingKey(item.key);
    setEditValue(item.value);
    setEditIsGlobal(item.isGlobal);
  };

  const handleSave = (key: string) => {
    updateConfig({ key, value: editValue, isGlobal: editIsGlobal });
    setEditingKey(null);
    setEditValue('');
  };

  const handleDelete = (key: string, isGlobal: boolean) => {
    deleteConfig({ key, isGlobal });
  };

  const handleCancel = () => {
    setEditingKey(null);
    setEditValue('');
    setIsAddingNew(false);
    setNewKey('');
    setNewValue('');
    setNewIsGlobal(false);
  };

  const handleAddNew = () => {
    if (newKey.trim() && newValue.trim()) {
      updateConfig({ key: newKey.trim(), value: newValue.trim(), isGlobal: newIsGlobal });
      setIsAddingNew(false);
      setNewKey('');
      setNewValue('');
      setNewIsGlobal(false);
    }
  };

  const renderValue = (item: ConfigItem) => {
    if (editingKey === item.key) {
      return (
        <div className="edit-value-container">
          <VSCodeTextField
            value={editValue}
            onInput={(e: any) => setEditValue(e.target.value)}
            className="edit-value-field"
            placeholder={l10n('valuePlaceholder')}
          />
          <VSCodeCheckbox
            checked={editIsGlobal}
            onChange={(e: any) => setEditIsGlobal(e.target.checked)}
          >
            {l10n('globalScope')}
          </VSCodeCheckbox>
        </div>
      );
    } else {
      return <code className="value-display">{item.value}</code>;
    }
  };

  const renderActions = (item: ConfigItem) => {
    if (editingKey === item.key) {
      return (
        <div className="action-buttons">
          <VSCodeButton onClick={() => handleSave(item.key)}>
            {l10n('saveButton')}
          </VSCodeButton>
          <VSCodeButton appearance="secondary" onClick={handleCancel}>
            {l10n('cancelButton')}
          </VSCodeButton>
        </div>
      );
    } else {
      return (
        <div className="action-buttons">
          <VSCodeButton appearance="icon" aria-label={l10n('editButton')} onClick={() => handleEdit(item)} title={l10n('editButton')}>
            <span className="codicon codicon-edit"></span>
          </VSCodeButton>
          <VSCodeButton appearance="icon" aria-label={l10n('deleteButton')} onClick={() => handleDelete(item.key, item.isGlobal)} title={l10n('deleteButton')}>
            <span className="codicon codicon-trash"></span>
          </VSCodeButton>
        </div>
      );
    }
  };

  return (
    <div className="config-container">
      <div className="header">
        <h1>{l10n('aspireConfigDescription')}</h1>
        
                {/* Display settings file paths */}
        {(localSettingsPath || globalSettingsPath) && (
          <>
            <div className="settings-paths">
              {localSettingsPath && (
                <div className="settings-path">
                  <span className="settings-path-label">{l10n('localSettingsPath')}: </span>
                  <code>{localSettingsPath}</code>
                </div>
              )}
              
              {globalSettingsPath && (
                <div className="settings-path">
                  <span className="settings-path-label">{l10n('globalSettingsPath')}: </span>
                  <code>{globalSettingsPath}</code>
                </div>
              )}
            </div>
            <VSCodeDivider />
          </>
        )}
        
        <div className="add-button-container">
          <VSCodeButton onClick={() => setIsAddingNew(true)} disabled={isAddingNew}>
            {l10n('addButton')}
          </VSCodeButton>
        </div>
      </div>
      
      {isAddingNew && (
        <div className="add-new-form">
          <h3>{l10n('addButton')}</h3>
          <div className="add-new-form-fields">
            <VSCodeTextField
              value={newKey}
              onInput={(e: any) => setNewKey(e.target.value)}
              placeholder={l10n('keyPlaceholder')}
            >
              {l10n('settingColumn')}
            </VSCodeTextField>
            <VSCodeTextField
              value={newValue}
              onInput={(e: any) => setNewValue(e.target.value)}
              placeholder={l10n('valuePlaceholder')}
            >
              {l10n('valueColumn')}
            </VSCodeTextField>
            <VSCodeCheckbox
              checked={newIsGlobal}
              onChange={(e: any) => setNewIsGlobal(e.target.checked)}
            >
              {l10n('globalScope')}
            </VSCodeCheckbox>
          </div>
          <div className="add-new-form-actions">
            <VSCodeButton onClick={handleAddNew}>
              {l10n('saveButton')}
            </VSCodeButton>
            <VSCodeButton appearance="secondary" onClick={handleCancel}>
              {l10n('cancelButton')}
            </VSCodeButton>
          </div>
        </div>
      )}

      {configItems.length === 0 ? (
        <p style={{ textAlign: 'center', padding: '32px', opacity: 0.7 }}>
          {l10n('noConfigMessage')}
        </p>
      ) : (
        <VSCodeDataGrid aria-label="Aspire Configuration">
          <VSCodeDataGridRow row-type="header">
            <VSCodeDataGridCell cell-type="columnheader" grid-column="1">
              {l10n('settingColumn')}
            </VSCodeDataGridCell>
            <VSCodeDataGridCell cell-type="columnheader" grid-column="2">
              {l10n('valueColumn')}
            </VSCodeDataGridCell>
            <VSCodeDataGridCell cell-type="columnheader" grid-column="3">
              {l10n('actionsColumn')}
            </VSCodeDataGridCell>
          </VSCodeDataGridRow>
          {configItems.map((item) => (
            <VSCodeDataGridRow key={item.key}>
              <VSCodeDataGridCell grid-column="1">
                <div style={{ display: 'flex', alignItems: 'center', gap: 'calc(var(--design-unit) * 2px)' }}>
                  <code style={{ fontWeight: 600 }}>{item.key}</code>
                  {item.isGlobal && (
                    <span style={{ 
                      fontSize: 'calc(var(--type-ramp-minus-1-font-size) * 1px)',
                      backgroundColor: 'var(--vscode-badge-background)',
                      color: 'var(--vscode-badge-foreground)',
                      padding: '2px 6px',
                      borderRadius: '3px'
                    }}>
                      {l10n('globalScope')}
                    </span>
                  )}
                </div>
              </VSCodeDataGridCell>
              <VSCodeDataGridCell 
                grid-column="2" 
                onDoubleClick={() => editingKey !== item.key && handleEdit(item)}
                style={{ cursor: editingKey !== item.key ? 'pointer' : 'default' }}
              >
                {renderValue(item)}
              </VSCodeDataGridCell>
              <VSCodeDataGridCell grid-column="3">
                {renderActions(item)}
              </VSCodeDataGridCell>
            </VSCodeDataGridRow>
          ))}
        </VSCodeDataGrid>
      )}
    </div>
  );
};
