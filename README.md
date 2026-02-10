# ReportingAgent (.NET 8)

该仓库包含一个 C# .NET Core（.NET 8）Agent 示例项目，用于通过 API 插件查询标准数据集定义，并根据用户上报需求由 LLM 驱动组装业务数据集定义。

## 项目结构

- `src/ReportingAgent/Program.cs`：最小 API 入口，提供 Agent 接口。
- `src/ReportingAgent/Services/ReportingAgentService.cs`：Agent 核心编排逻辑与系统提示词。
- `src/ReportingAgent/Clients/OpenAiClient.cs`：OpenAI 模型调用客户端。
- `src/ReportingAgent/Clients/CollectionSchemaClient.cs`：标准数据集定义插件客户端。
- `src/ReportingAgent/Clients/DataQueryClient.cs`：业务数据查询插件客户端。
- `src/ReportingAgent/Models/CollectionModels.cs`：领域模型。

## API

### 1) 获取系统提示词

`GET /agent/system-prompt`

### 2) 组装业务数据集

`POST /agent/build-business-dataset`

示例请求：

```json
{
  "requirementText": "需要按机构和日期统计门诊费用并关联患者信息",
  "preferredInputs": {
    "OrgCode": "1001",
    "StartDate": "2025-01-01",
    "EndDate": "2025-01-31"
  }
}
```

## 运行

```bash
cd src/ReportingAgent
dotnet restore
dotnet run
```

## 配置

在 `appsettings.json` 中配置插件地址和 OpenAI 参数：

- `PluginApis.CollectionSchemaBaseUrl`
- `PluginApis.DataQueryBaseUrl`
- `OpenAI.BaseUrl`
- `OpenAI.ApiKey`
- `OpenAI.Model`
- `OpenAI.Temperature`

> 未配置 `OpenAI.ApiKey` 时，服务会降级到规则匹配策略继续执行。

## 编排流程

1. 查询标准数据集元数据（集合、参数、值域）
2. 将需求 + 元数据送入 OpenAI 生成业务数据集草案（JSON）
3. 对草案进行规范化和兜底修复（防止越界字段）
4. 对输入参数进行采样调用
5. 执行业务查询验证并返回结果

## 插件端点约定

- `CollectionSchema/GetAllCollections`
- `CollectionSchema/GetAllParameters`
- `CollectionSchema/GetAllValueSets`
- `DataQuery/SampleCollectionData`
- `DataQuery/ExecuteBusinessQuery`

以上端点默认通过 `appsettings.json` 中 `PluginApis` 配置指定。
