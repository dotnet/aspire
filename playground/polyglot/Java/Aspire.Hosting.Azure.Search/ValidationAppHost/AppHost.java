package aspire;

import java.util.Map;

final class AppHost {

    void main() throws Exception {
        var builder = DistributedApplication.CreateBuilder();
        var search = builder.addAzureSearch("search");
        search.withSearchRoleAssignments(search, new AzureSearchRole[] { AzureSearchRole.SEARCH_SERVICE_CONTRIBUTOR, AzureSearchRole.SEARCH_INDEX_DATA_READER });
        builder.build().run();
    }
}
