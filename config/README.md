# Configurações — Navisworks Toolkit

## `config.json`

Fonte única de configuração declarativa da plataforma, consumida pelos **scripts de build/deploy** (`build.ps1`, `deploy.ps1`).

> **Importante:** o plugin compilado **não lê configuração em runtime**. Nenhum dos dois projetos originais consumia `App.config`/`Config.json` em execução — o `App.config` do Auto_ViewTool declarava `appSettings` (PluginName/Version) que nunca eram lidos pelo código (plugins rodam no processo do Navisworks, que não carrega o App.config do projeto). Esses valores foram preservados na seção `legacy`. Introduzir leitura de configuração em runtime alteraria comportamento e ficou registrado como melhoria futura no MigrationReport.

### Seções

| Seção | Conteúdo | Consumidor |
|---|---|---|
| `product` | Nome, assembly, versão (SemVer), developer ID do atributo `[Plugin]` | Scripts e documentação |
| `paths.navisworksSdk` | Pasta do Navisworks com as DLLs da API (HintPath do csproj) | Referência/diagnóstico |
| `paths.pluginInstallDir` | Destino de instalação do plugin (subpasta com o nome da DLL — exigência do Navisworks) | `deploy.ps1` |
| `paths.buildOutput` | Saída do build Release | `deploy.ps1` |
| `paths.perfLog` | Localização do log de diagnóstico do PerfLog (fixa no código, documentada aqui) | Diagnóstico |
| `featureFlags` | Um flag por módulo — **informativos** (a DLL atual sempre registra os 6 comandos) | Futura modularização |
| `legacy` | Valores históricos do `App.config` do Auto_ViewTool | Preservação |

### Configurações em runtime que continuam onde estavam (comportamento preservado)

| Item | Local | Origem |
|---|---|---|
| Log de performance | `%TEMP%\AutoViewTool_perf.log` (caminho fixo em `src/Core/PerfLog.cs`) | Auto_ViewTool |
| Persistência de sets gravados | Atributos custom no próprio modelo (categoria `Verum_Attributes`, ver `src/Shared/VerumSchema.cs`) | SetAtributesToolkit |
| Templates de atributos | Arquivos `.csv`/`.xml` escolhidos pelo usuário | SetAtributesToolkit |
| Regras de coloração | Arquivos `.xml` de import/export escolhidos pelo usuário | SetAtributesToolkit |
| Template de viewpoints | `.xlsx` gerado/importado pelo usuário | Auto_ViewTool |
