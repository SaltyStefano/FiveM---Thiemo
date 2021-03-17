ESX = nil
TriggerEvent('mrpx:getSharedObject', function(obj) ESX = obj end)

RegisterServerEvent("mrpx_slots:BetsAndMoney")
AddEventHandler("mrpx_slots:BetsAndMoney", function(bets)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        local xItem = xPlayer.getInventoryItem('zetony')
        if xItem.count < 10 then
            TriggerClientEvent('mrpx:showNotification', _source, "Je hebt minimaal 100 Casino Chips nodig om te spelen")
        else
            MySQL.Sync.execute("UPDATE users SET zetony=@zetony WHERE identifier=@identifier",{['@identifier'] = xPlayer.identifier, ['@zetony'] = xItem.count})
            TriggerClientEvent("mrpx_slots:UpdateSlots", _source, xItem.count)
            xPlayer.removeInventoryItem('zetony', xItem.count)
        end
    end
end)

RegisterServerEvent("mrpx_slots:updateCoins")
AddEventHandler("mrpx_slots:updateCoins", function(bets)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        MySQL.Sync.execute("UPDATE users SET zetony=@zetony WHERE identifier=@identifier",{['@identifier'] = xPlayer.identifier, ['@zetony'] = bets})
    end
end)

RegisterServerEvent("mrpx_slots:PayOutRewards")
AddEventHandler("mrpx_slots:PayOutRewards", function(amount)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        amount = math.floor(tonumber(amount))
        if amount > 0 then
            xPlayer.addInventoryItem('zetony', amount)
        end
        MySQL.Sync.execute("UPDATE users SET zetony=0 WHERE identifier=@identifier",{['@identifier'] = xPlayer.identifier})
    end
end)

RegisterServerEvent("route68_kasyno:WymienZetony")
AddEventHandler("route68_kasyno:WymienZetony", function(count)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        local xItem = xPlayer.getInventoryItem('zetony')
        if xItem.count < count then
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je hebt niet dat aantal aan Casino Chips'})
        elseif xItem.count >= count then
            local kwota = math.floor(count * 5)
            xPlayer.removeInventoryItem('zetony', count)
            xPlayer.addMoney(kwota)
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je kreeg €'..kwota..' voor '..count..' Casino Chips.'})
        end
    end
end)

RegisterServerEvent("route68_kasyno:KupZetony")
AddEventHandler("route68_kasyno:KupZetony", function(count)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        local cash = xPlayer.getMoney()
        local kwota = math.floor(count * 5)
        if kwota > cash then
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je hebt niet genoeg geld'})
        elseif kwota <= cash then
            xPlayer.addInventoryItem('zetony', count)
            xPlayer.removeMoney(kwota)
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je kreeg '..count..' Casino Chips voor €'..kwota..'.'})
        end
    end
end)

RegisterServerEvent("route68_kasyno:KupAlkohol")
AddEventHandler("route68_kasyno:KupAlkohol", function(count, item)
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    if xPlayer then
        local cash = xPlayer.getMoney()
        local kwota = math.floor(count * 10)
        if kwota > cash then
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je hebt niet genoeg geld'})
        elseif kwota <= cash then
            xPlayer.addInventoryItem(item, count)
            xPlayer.removeMoney(kwota)
            TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je kreeg '..count..' items voor €'..count..'.'})
        end
    end
end)

RegisterServerEvent("route68_kasyno:getJoinChips")
AddEventHandler("route68_kasyno:getJoinChips", function()
    local _source   = source
    local xPlayer   = ESX.GetPlayerFromId(_source)
    local identifier = xPlayer.identifier
    MySQL.Async.fetchAll('SELECT zetony FROM users WHERE @identifier=identifier', {
		['@identifier'] = identifier
	}, function(result)
		if result[1] then
            local zetony = result[1].zetony
            if zetony > 0 then
                TriggerClientEvent('pNotify:SendNotification', _source, {text = 'Je kreeg '..tostring(zetony)..' Casino Chips, omdat je het spel verliet.'})
                xPlayer.addInventoryItem('zetony', zetony)
                MySQL.Sync.execute("UPDATE users SET zetony=0 WHERE identifier=@identifier",{['@identifier'] = xPlayer.identifier})
            end
		end
	end)
end)