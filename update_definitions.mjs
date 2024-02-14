import fs from 'node:fs/promises';

const opcodesUrl = 'https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json';

function getOpcode(list, messageType) {
    const { opcode } = list.find((o) => o.name === messageType);
    return opcode;
}

fetch(opcodesUrl)
    .then((res) => res.json())
    .then((res) => res.find((regionLists) => regionLists.region === 'Global').lists)
    .then((lists) => ({
        ClientTrigger: getOpcode(lists.ClientZoneIpcType, 'ClientTrigger'),
        PlayerSpawn: getOpcode(lists.ServerZoneIpcType, 'PlayerSpawn'),
        PlayerSetup: getOpcode(lists.ServerZoneIpcType, 'PlayerSetup'),
        ItemMarketBoardInfo: getOpcode(lists.ServerZoneIpcType, 'ItemMarketBoardInfo'),
        MarketBoardItemRequestStart: getOpcode(lists.ServerZoneIpcType, 'MarketBoardItemListingCount'),
        MarketBoardOfferings: getOpcode(lists.ServerZoneIpcType, 'MarketBoardItemListing'),
        MarketBoardHistory: getOpcode(lists.ServerZoneIpcType, 'MarketBoardItemListingHistory'),
        MarketTaxRates: getOpcode(lists.ServerZoneIpcType, 'ResultDialog'),
        ContentIdNameMapResp: -1,
    }))
    .then(async (res) => {
        console.log(res);
        await fs.writeFile('definitions.json', JSON.stringify(res) + '\n');
    })
    .catch(console.error);
