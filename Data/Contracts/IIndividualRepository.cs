﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Entities.PersonModels;
using Microsoft.EntityFrameworkCore;

namespace Data.Contracts
{
    public interface IIndividualRepository : IRepository<CreIndividual>
    {
        
    }
}
