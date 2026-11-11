using AAEmu.Game.Models.Game.Char;

namespace AAEmu.Game.Core.Managers;

public interface IFamilyManager : ILoadable
{
    void SaveAllFamilies();
    void OnCharacterLogin(Character character);
    void OnCharacterLogout(Character character);
    void OnCharacterRefresh(Character character);
    bool SendChatMessage(Character character, string message, int ability, byte languageType);
    void LeaveFamily(Character character);
    void RemoveDeletedCharacter(Character character);
    void SetName(Character owner, string name);
    void IncreaseMemberLimit(Character owner);
    void SetNotice(Character owner, string notice);
    void ChangeMemberRole(Character owner, uint memberId, uint roleId);
    void AddExperience(Character source, uint amount);
    bool TryLevelUp(Character owner, uint targetLevel);
    IDisposable AcquireCharacterDeletionLock();
    bool CanDeleteCharacterLocked(uint characterId, uint storedFamilyId);
    /// <summary>
    /// Family id of an online or offline character, 0 when it has none. Packet writes call this under a
    /// connection send lock, so an implementation must not wait for a lock that is held while sending.
    /// </summary>
    uint GetFamilyOfCharacter(uint characterId);
}
